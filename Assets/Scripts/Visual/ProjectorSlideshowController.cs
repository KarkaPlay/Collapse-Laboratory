// File: Scripts/ProjectorSlideshowController.cs
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[DisallowMultipleComponent]
public class ProjectorSlideshowController : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // Inspector Parameters
    // ─────────────────────────────────────────────

    [Header("═══ Slide Content ═══")]
    [Tooltip("Массив текстур-слайдов для проецирования")]
    [SerializeField] private Texture2D[] slides;

    [Tooltip("Длительность показа одного слайда (секунды)")]
    [SerializeField, Range(1f, 60f)] private float slideDuration = 5f;

    [Tooltip("Длительность перехода между слайдами (секунды)")]
    [SerializeField, Range(0f, 3f)] private float transitionDuration = 0.8f;

    [Header("═══ Projection Settings ═══")]
    [Tooltip("Яркость проекции")]
    [SerializeField, Range(0f, 5f)] private float brightness = 1.5f;

    [Tooltip("Контрастность изображения")]
    [SerializeField, Range(0.5f, 3f)] private float contrast = 1.1f;

    [Tooltip("Цвет лампы проектора (тёплый для 1980-х)")]
    [SerializeField] private Color lampColor = new Color(1f, 0.93f, 0.82f, 1f);

    [Tooltip("Мягкость краёв проекции")]
    [SerializeField, Range(0.01f, 0.5f)] private float edgeSoftness = 0.08f;

    [Tooltip("Сила оптического искажения линзы")]
    [SerializeField, Range(0f, 0.05f)] private float distortionStrength = 0.008f;

    [Tooltip("Сила виньетки по краям")]
    [SerializeField, Range(0f, 2f)] private float vignetteStrength = 0.6f;

    [Header("═══ Atmosphere ═══")]
    [Tooltip("Интенсивность аналогового мерцания")]
    [SerializeField, Range(0f, 0.3f)] private float flickerIntensity = 0.05f;

    [Tooltip("Интенсивность пыли/зерна")]
    [SerializeField, Range(0f, 0.15f)] private float dustIntensity = 0.03f;

    [Tooltip("Включить volumetric beam")]
    [SerializeField] private bool enableBeam = true;

    [Header("═══ Projection Frustum ═══")]
    [Tooltip("Угол обзора проектора (градусы)")]
    [SerializeField, Range(10f, 90f)] private float projectorFOV = 40f;

    [Tooltip("Соотношение сторон проекции")]
    [SerializeField] private float projectorAspect = 1.333f;

    [Tooltip("Ближняя плоскость отсечения")]
    [SerializeField] private float nearClip = 0.3f;

    [Tooltip("Дальняя плоскость отсечения")]
    [SerializeField] private float farClip = 12f;

    [Header("═══ References ═══")]
    [Tooltip("MeshRenderer frustum-бокса с проекционным материалом")]
    [SerializeField] private MeshRenderer projectionRenderer;

    [Tooltip("MeshRenderer конуса луча (beam)")]
    [SerializeField] private MeshRenderer beamRenderer;

    [Tooltip("Spot Light для подсветки (опционально)")]
    [SerializeField] private Light spotLight;

    // ─────────────────────────────────────────────
    // Private state
    // ─────────────────────────────────────────────

    private MaterialPropertyBlock _projectionMPB;
    private MaterialPropertyBlock _beamMPB;

    private int _currentSlideIndex;
    private int _nextSlideIndex;
    private float _slideTimer;
    private float _transitionProgress;
    private bool _isTransitioning;

    // Shader property IDs (кешируем, чтобы избежать аллокаций)
    private static readonly int ID_SlideTexA = Shader.PropertyToID("_SlideTexA");
    private static readonly int ID_SlideTexB = Shader.PropertyToID("_SlideTexB");
    private static readonly int ID_TransitionBlend = Shader.PropertyToID("_TransitionBlend");
    private static readonly int ID_Brightness = Shader.PropertyToID("_Brightness");
    private static readonly int ID_Contrast = Shader.PropertyToID("_Contrast");
    private static readonly int ID_LampColor = Shader.PropertyToID("_LampColor");
    private static readonly int ID_EdgeSoftness = Shader.PropertyToID("_EdgeSoftness");
    private static readonly int ID_DistortionStr = Shader.PropertyToID("_DistortionStrength");
    private static readonly int ID_VignetteStr = Shader.PropertyToID("_VignetteStrength");
    private static readonly int ID_FlickerIntensity = Shader.PropertyToID("_FlickerIntensity");
    private static readonly int ID_DustIntensity = Shader.PropertyToID("_DustIntensity");
    private static readonly int ID_FalloffStart = Shader.PropertyToID("_FalloffStart");
    private static readonly int ID_FalloffEnd = Shader.PropertyToID("_FalloffEnd");
    private static readonly int ID_ProjectorVP = Shader.PropertyToID("_ProjectorVP");
    private static readonly int ID_ProjectorPos = Shader.PropertyToID("_ProjectorPosition");
    private static readonly int ID_ProjectorFwd = Shader.PropertyToID("_ProjectorForward");
    private static readonly int ID_BeamIntensity = Shader.PropertyToID("_BeamIntensity");

    // ─────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────

    private void OnEnable()
    {
        _projectionMPB = new MaterialPropertyBlock();
        _beamMPB = new MaterialPropertyBlock();
        _currentSlideIndex = 0;
        _nextSlideIndex = 0;
        _slideTimer = 0f;
        _isTransitioning = false;

        UpdateAllProperties();
    }

    // Используем LateUpdate для синхронизации с камерой
    private void LateUpdate()
    {
        float dt = Application.isPlaying ? Time.deltaTime : 0.016f;

        if (slides == null || slides.Length == 0) return;

        // ── Таймер слайдов ──
        if (Application.isPlaying)
        {
            _slideTimer += dt;

            if (_isTransitioning)
            {
                _transitionProgress += dt / Mathf.Max(transitionDuration, 0.01f);

                if (_transitionProgress >= 1f)
                {
                    _transitionProgress = 0f;
                    _isTransitioning = false;
                    _currentSlideIndex = _nextSlideIndex;
                    _slideTimer = 0f;
                }
            }
            else if (_slideTimer >= slideDuration)
            {
                StartTransition((_currentSlideIndex + 1) % slides.Length);
            }
        }

        UpdateAllProperties();
    }

    // ─────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────

    /// <summary>
    /// Вручную перейти к следующему слайду.
    /// </summary>
    public void NextSlide()
    {
        if (slides == null || slides.Length <= 1) return;
        StartTransition((_currentSlideIndex + 1) % slides.Length);
    }

    /// <summary>
    /// Вручную перейти к предыдущему слайду.
    /// </summary>
    public void PreviousSlide()
    {
        if (slides == null || slides.Length <= 1) return;
        StartTransition((_currentSlideIndex - 1 + slides.Length) % slides.Length);
    }

    /// <summary>
    /// Перейти к конкретному слайду по индексу.
    /// </summary>
    public void GoToSlide(int index)
    {
        if (slides == null || slides.Length == 0) return;
        index = Mathf.Clamp(index, 0, slides.Length - 1);
        if (index == _currentSlideIndex && !_isTransitioning) return;
        StartTransition(index);
    }

    /// <summary>
    /// Текущий индекс слайда.
    /// </summary>
    public int CurrentSlideIndex => _currentSlideIndex;

    /// <summary>
    /// Общее количество слайдов.
    /// </summary>
    public int SlideCount => slides?.Length ?? 0;

    // ─────────────────────────────────────────────
    // Internal
    // ─────────────────────────────────────────────

    private void StartTransition(int targetIndex)
    {
        if (_isTransitioning) // Завершаем текущий переход
        {
            _currentSlideIndex = _nextSlideIndex;
        }

        _nextSlideIndex = targetIndex;
        _transitionProgress = 0f;
        _isTransitioning = true;
    }

    private void UpdateAllProperties()
    {
        if (projectionRenderer == null) return;

        // ── Матрица проекции ──
        // Строим View и Projection матрицы проектора,
        // как если бы это была камера

        Matrix4x4 projectorView = Matrix4x4.TRS(
            transform.position,
            transform.rotation,
            Vector3.one
        ).inverse;

        Matrix4x4 projectorProj = Matrix4x4.Perspective(
            projectorFOV, projectorAspect, nearClip, farClip
        );

        // Коррекция для платформ с reversed-Z
        // Unity передаёт GL-стиль projection,
        // но нам нужна стандартная для UV маппинга
        Matrix4x4 projectorVP = projectorProj * projectorView;

        // ── Projection MPB ──
        projectionRenderer.GetPropertyBlock(_projectionMPB);

        // Текстуры
        if (slides != null && slides.Length > 0)
        {
            Texture2D texA = slides[_currentSlideIndex];
            Texture2D texB = _isTransitioning && slides.Length > _nextSlideIndex
                ? slides[_nextSlideIndex]
                : texA;

            if (texA != null)
                _projectionMPB.SetTexture(ID_SlideTexA, texA);
            if (texB != null)
                _projectionMPB.SetTexture(ID_SlideTexB, texB);
        }

        float smoothBlend = _isTransitioning
            ? Mathf.SmoothStep(0f, 1f, _transitionProgress)
            : 0f;

        _projectionMPB.SetFloat(ID_TransitionBlend, smoothBlend);
        _projectionMPB.SetFloat(ID_Brightness, brightness);
        _projectionMPB.SetFloat(ID_Contrast, contrast);
        _projectionMPB.SetColor(ID_LampColor, lampColor);
        _projectionMPB.SetFloat(ID_EdgeSoftness, edgeSoftness);
        _projectionMPB.SetFloat(ID_DistortionStr, distortionStrength);
        _projectionMPB.SetFloat(ID_VignetteStr, vignetteStrength);
        _projectionMPB.SetFloat(ID_FlickerIntensity, flickerIntensity);
        _projectionMPB.SetFloat(ID_DustIntensity, dustIntensity);
        _projectionMPB.SetFloat(ID_FalloffStart, nearClip);
        _projectionMPB.SetFloat(ID_FalloffEnd, farClip);
        _projectionMPB.SetMatrix(ID_ProjectorVP, projectorVP);
        _projectionMPB.SetVector(ID_ProjectorPos, transform.position);
        _projectionMPB.SetVector(ID_ProjectorFwd, transform.forward);

        projectionRenderer.SetPropertyBlock(_projectionMPB);

        // ── Beam MPB ──
        if (beamRenderer != null)
        {
            beamRenderer.enabled = enableBeam;

            if (enableBeam)
            {
                beamRenderer.GetPropertyBlock(_beamMPB);
                _beamMPB.SetFloat(ID_BeamIntensity,
                    brightness * 0.1f * (flickerIntensity > 0 ? 1f : 0.8f));
                _beamMPB.SetColor(ID_LampColor, lampColor);
                beamRenderer.SetPropertyBlock(_beamMPB);
            }
        }

        // ── Spot Light синхронизация ──
        if (spotLight != null)
        {
            spotLight.color = lampColor;
            spotLight.intensity = brightness * 0.5f;
            spotLight.spotAngle = projectorFOV;
            spotLight.range = farClip;
        }
    }

    // ─────────────────────────────────────────────
    // Gizmos для визуализации frustum в Editor
    // ─────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.9f, 0.5f, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;

        // Приблизительная визуализация frustum
        float halfAngleRad = projectorFOV * 0.5f * Mathf.Deg2Rad;
        float farHalfH = Mathf.Tan(halfAngleRad) * farClip;
        float farHalfW = farHalfH * projectorAspect;
        float nearHalfH = Mathf.Tan(halfAngleRad) * nearClip;
        float nearHalfW = nearHalfH * projectorAspect;

        // Near plane corners
        Vector3 n0 = new Vector3(-nearHalfW, -nearHalfH, nearClip);
        Vector3 n1 = new Vector3(nearHalfW, -nearHalfH, nearClip);
        Vector3 n2 = new Vector3(nearHalfW, nearHalfH, nearClip);
        Vector3 n3 = new Vector3(-nearHalfW, nearHalfH, nearClip);

        // Far plane corners
        Vector3 f0 = new Vector3(-farHalfW, -farHalfH, farClip);
        Vector3 f1 = new Vector3(farHalfW, -farHalfH, farClip);
        Vector3 f2 = new Vector3(farHalfW, farHalfH, farClip);
        Vector3 f3 = new Vector3(-farHalfW, farHalfH, farClip);

        // Near plane
        Gizmos.DrawLine(n0, n1); Gizmos.DrawLine(n1, n2);
        Gizmos.DrawLine(n2, n3); Gizmos.DrawLine(n3, n0);

        // Far plane
        Gizmos.DrawLine(f0, f1); Gizmos.DrawLine(f1, f2);
        Gizmos.DrawLine(f2, f3); Gizmos.DrawLine(f3, f0);

        // Edges
        Gizmos.DrawLine(n0, f0); Gizmos.DrawLine(n1, f1);
        Gizmos.DrawLine(n2, f2); Gizmos.DrawLine(n3, f3);

        // Center ray
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.5f);
        Gizmos.DrawLine(Vector3.zero, new Vector3(0, 0, farClip));
    }
#endif
}