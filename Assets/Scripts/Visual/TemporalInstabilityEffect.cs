// ============================================================================
// TemporalInstabilityEffect.cs
// Визуальный эффект «временной нестабильности» объекта
// Unity 6 (URP)
// ============================================================================

using UnityEngine;

namespace TemporalFX
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshRenderer))]
    public class TemporalInstabilityEffect : MonoBehaviour
    {
        // =====================================================================
        // INSPECTOR
        // =====================================================================

        [Header("=== Effect Control ===")]

        [Tooltip("Включить/выключить эффект")]
        [SerializeField] private bool _effectEnabled = true;

        [Tooltip("Общая интенсивность эффекта (0 = выключен, 1 = максимум)")]
        [Range(0f, 1f)]
        [SerializeField] private float _intensity = 1f;

        [Header("=== Distortion ===")]

        [Tooltip("Скорость анимации деформации")]
        [Range(0.1f, 10f)]
        [SerializeField] private float _speed = 1f;

        [Tooltip("Плавная деформация — смещение вершин по нормали + пульсация")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _warpAmount = 0.01f;

        [Tooltip("Резкие подёргивания — glitch-эффект")]
        [Range(0f, 1f)]
        [SerializeField] private float _glitchAmount = 0.1f;

        [Tooltip("Масштаб шума — влияет на 'зернистость' деформации")]
        [Range(0.1f, 20f)]
        [SerializeField] private float _noiseScale = 3.0f;

        [Header("=== Color Shift ===")]

        [Tooltip("Сила хроматического сдвига цвета")]
        [Range(0f, 1f)]
        [SerializeField] private float _colorShiftAmount = 0.1f;

        [Tooltip("Цвет свечения нестабильности")]
        [SerializeField]
        private Color _glowColor =
            new Color(0.28f, 0.28f, 0.5f, 1.0f);

        [Header("=== Uniqueness ===")]

        [Tooltip("Уникальное смещение для этого объекта")]
        [SerializeField] private float _seed = 0f;

        [Tooltip("Автоматически генерировать seed на основе позиции")]
        [SerializeField] private bool _autoSeed = true;

        [Header("=== Anomaly Surge ===")]

        [Tooltip("Включить режим 'аномалия усиливается'")]
        [SerializeField] private bool _surgeEnabled = false;

        [Tooltip("Длительность всплеска интенсивности")]
        [Range(0.5f, 5f)]
        [SerializeField] private float _surgeDuration = 2.0f;

        [Tooltip("Множитель интенсивности при всплеске")]
        [Range(1f, 5f)]
        [SerializeField] private float _surgeMultiplier = 3.0f;

        [Header("=== Global Sync ===")]

        [Tooltip("Использовать глобальный параметр _GlobalTemporalIntensity")]
        [SerializeField] private bool _useGlobalSync = false;

        // =====================================================================
        // CACHED REFERENCES
        // =====================================================================

        private MeshRenderer _renderer;
        private MaterialPropertyBlock _propBlock;

        // Shader property IDs — обновлены под новые имена
        private static readonly int PropIntensity =
            Shader.PropertyToID("_TemporalIntensity");
        private static readonly int PropSpeed =
            Shader.PropertyToID("_TemporalSpeed");
        private static readonly int PropWarpAmount =
            Shader.PropertyToID("_WarpAmount");
        private static readonly int PropGlitchAmount =
            Shader.PropertyToID("_GlitchAmount");
        private static readonly int PropNoiseScale =
            Shader.PropertyToID("_NoiseScale");
        private static readonly int PropColorShift =
            Shader.PropertyToID("_ColorShiftAmount");
        private static readonly int PropGlowColor =
            Shader.PropertyToID("_GlowColor");
        private static readonly int PropSeed =
            Shader.PropertyToID("_InstanceSeed");
        private static readonly int PropEffectEnabled =
            Shader.PropertyToID("_EffectEnabled");

        // Surge state
        private float _surgeTimer = 0f;
        private bool _isSurging = false;
        private float _currentSurgeMultiplier = 1f;

        // =====================================================================
        // LIFECYCLE
        // =====================================================================

        private void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
            _propBlock = new MaterialPropertyBlock();

            if (_autoSeed && _seed == 0f)
            {
                Vector3 pos = transform.position;
                _seed = Mathf.Abs(
                    pos.x * 73.856f + pos.y * 29.347f
                    + pos.z * 51.923f) % 1000f;
            }
        }

        private void OnEnable()
        {
            ApplyProperties();
        }

        private void OnDisable()
        {
            if (_renderer != null)
            {
                _renderer.GetPropertyBlock(_propBlock);
                _propBlock.SetFloat(PropEffectEnabled, 0f);
                _propBlock.SetFloat(PropIntensity, 0f);
                _renderer.SetPropertyBlock(_propBlock);
            }
        }

        private void Update()
        {
            // Surge processing
            if (_isSurging)
            {
                _surgeTimer -= Time.deltaTime;
                if (_surgeTimer <= 0f)
                {
                    _isSurging = false;
                    _currentSurgeMultiplier = 1f;
                }
                else
                {
                    float t = _surgeTimer / _surgeDuration;
                    _currentSurgeMultiplier =
                        1f + (_surgeMultiplier - 1f)
                        * SmootherStep(t);
                }
            }

            ApplyProperties();
        }

        // =====================================================================
        // CORE
        // =====================================================================

        private void ApplyProperties()
        {
            if (_renderer == null) return;

            _renderer.GetPropertyBlock(_propBlock);

            float finalIntensity = _effectEnabled
                ? _intensity * _currentSurgeMultiplier
                : 0f;

            if (_useGlobalSync)
            {
                float globalIntensity =
                    Shader.GetGlobalFloat("_GlobalTemporalIntensity");
                finalIntensity *= globalIntensity;
            }

            // Clamp intensity but allow surge to push above 1
            // (шейдер использует effectMask = _EffectEnabled * _TemporalIntensity,
            //  значения > 1 усилят все эффекты пропорционально)
            finalIntensity = Mathf.Max(0f, finalIntensity);

            _propBlock.SetFloat(PropEffectEnabled,
                _effectEnabled ? 1f : 0f);
            _propBlock.SetFloat(PropIntensity, finalIntensity);
            _propBlock.SetFloat(PropSpeed, _speed);
            _propBlock.SetFloat(PropWarpAmount, _warpAmount);
            _propBlock.SetFloat(PropGlitchAmount, _glitchAmount);
            _propBlock.SetFloat(PropNoiseScale, _noiseScale);
            _propBlock.SetFloat(PropColorShift, _colorShiftAmount);
            _propBlock.SetColor(PropGlowColor, _glowColor);
            _propBlock.SetFloat(PropSeed, _seed);

            _renderer.SetPropertyBlock(_propBlock);
        }

        // =====================================================================
        // PUBLIC API
        // =====================================================================

        public void TriggerSurge()
        {
            if (!_surgeEnabled) return;
            _isSurging = true;
            _surgeTimer = _surgeDuration;
            _currentSurgeMultiplier = _surgeMultiplier;
        }

        public void TriggerSurge(float duration, float multiplier)
        {
            _isSurging = true;
            _surgeTimer = duration;
            _surgeDuration = duration;
            _surgeMultiplier = multiplier;
            _currentSurgeMultiplier = multiplier;
        }

        public void SetIntensity(float value)
        {
            _intensity = Mathf.Clamp01(value);
        }

        public void SetEffectEnabled(bool enabled)
        {
            _effectEnabled = enabled;
        }

        /// <summary>
        /// Установить силу плавной деформации.
        /// </summary>
        public void SetWarpAmount(float value)
        {
            _warpAmount = Mathf.Clamp(value, 0f, 0.5f);
        }

        /// <summary>
        /// Установить силу подёргиваний.
        /// </summary>
        public void SetGlitchAmount(float value)
        {
            _glitchAmount = Mathf.Clamp(value, 0f, 1f);
        }

        public bool IsSurging => _isSurging;

        // =====================================================================
        // UTILITY
        // =====================================================================

        private static float SmootherStep(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_renderer == null)
                _renderer = GetComponent<MeshRenderer>();
            if (_propBlock == null)
                _propBlock = new MaterialPropertyBlock();

            // Работаем и в Edit Mode для превью
            if (_renderer != null)
                ApplyProperties();
        }

        private void Reset()
        {
            _intensity = 0.5f;
            _speed = 1.5f;
            _warpAmount = 0.05f;
            _glitchAmount = 0.1f;
            _noiseScale = 3.0f;
            _colorShiftAmount = 0.15f;
            _glowColor = new Color(0.4f, 0.5f, 1.0f, 1.0f);
            _autoSeed = true;
        }
#endif
    }
}