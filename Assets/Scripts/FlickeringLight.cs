using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Flickering Light — компонент мерцающего света для Unity 6 URP.
/// Поддерживает несколько режимов мерцания, два значения яркости,
/// опциональное изменение цвета, радиуса и Emissive-параметра материала.
/// </summary>
[RequireComponent(typeof(Light))]
public class FlickeringLight : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  ENUMS
    // ──────────────────────────────────────────────

    public enum FlickerMode
    {
        Random,
        SineWave,
        Candle,
        Strobe,
        Pulse
    }

    // ──────────────────────────────────────────────
    //  ОСНОВНЫЕ ПАРАМЕТРЫ
    // ──────────────────────────────────────────────

    [Header("═══ Режим мерцания ═══")]
    [Tooltip("Выберите алгоритм мерцания")]
    public FlickerMode mode = FlickerMode.Candle;

    [Header("═══ Яркость ═══")]
    [Tooltip("Минимальная яркость (intensity)")]
    [Min(0f)]
    public float brightnessA = 0.5f;

    [Tooltip("Максимальная яркость (intensity)")]
    [Min(0f)]
    public float brightnessB = 2.0f;

    [Header("═══ Скорость ═══")]
    [Tooltip("Общая скорость мерцания")]
    [Range(0.1f, 50f)]
    public float speed = 5f;

    [Tooltip("Сглаживание переходов (0 = резко, 1 = очень плавно)")]
    [Range(0f, 1f)]
    public float smoothing = 0.5f;

    // ──────────────────────────────────────────────
    //  РАНДОМИЗАЦИЯ
    // ──────────────────────────────────────────────

    [Header("═══ Рандомизация ═══")]
    [Tooltip("Случайный сдвиг Perlin-шума для каждого экземпляра")]
    public bool randomizeOffset = true;

    [Tooltip("Случайное отклонение скорости (±%)")]
    [Range(0f, 50f)]
    public float speedVariation = 10f;

    // ──────────────────────────────────────────────
    //  ЦВЕТ (опционально)
    // ──────────────────────────────────────────────

    [Header("═══ Цвет (опционально) ═══")]
    [Tooltip("Включить переключение между двумя цветами")]
    public bool enableColorShift = false;

    [Tooltip("Первый цвет")]
    public Color colorA = new Color(1f, 0.85f, 0.6f);

    [Tooltip("Второй цвет")]
    public Color colorB = new Color(1f, 0.5f, 0.2f);

    // ──────────────────────────────────────────────
    //  РАДИУС (опционально)
    // ──────────────────────────────────────────────

    [Header("═══ Радиус (опционально) ═══")]
    [Tooltip("Включить изменение радиуса вместе с мерцанием")]
    public bool enableRangeFlicker = false;

    [Tooltip("Минимальный радиус")]
    [Min(0f)]
    public float rangeA = 5f;

    [Tooltip("Максимальный радиус")]
    [Min(0f)]
    public float rangeB = 10f;

    // ──────────────────────────────────────────────
    //  EMISSIVE МАТЕРИАЛА
    // ──────────────────────────────────────────────

    [Header("═══ Emissive материала ═══")]
    [Tooltip("Перетащите сюда объект (Renderer), у которого нужно менять Emission")]
    public Renderer emissiveTarget;

    [Tooltip("Включить синхронизацию Emission с мерцанием")]
    public bool enableEmissiveFlicker = true;

    [Tooltip("Базовый HDR-цвет Emission")]
    [ColorUsage(false, true)]
    public Color emissiveColor = new Color(1f, 0.7f, 0.3f, 1f);

    [Tooltip("Минимальный множитель яркости Emission (при brightnessA)")]
    [Min(0f)]
    public float emissiveIntensityMin = 0.2f;

    [Tooltip("Максимальный множитель яркости Emission (при brightnessB)")]
    [Min(0f)]
    public float emissiveIntensityMax = 3.0f;

    // ──────────────────────────────────────────────
    //  STROBE
    // ──────────────────────────────────────────────

    [Header("═══ Strobe ═══")]
    [Tooltip("Доля цикла, когда свет включён (0..1)")]
    [Range(0.05f, 0.95f)]
    public float strobeDuty = 0.5f;

    // ──────────────────────────────────────────────
    //  PRIVATE
    // ──────────────────────────────────────────────

    private Light _light;
    private UniversalAdditionalLightData _urpData;
    private float _noiseOffset;
    private float _currentIntensity;
    private float _targetIntensity;
    private float _effectiveSpeed;
    private float _timer;

    // Emissive
    private MaterialPropertyBlock _emissiveMPB;
    private float _currentEmissiveIntensity;

    private static readonly int PropEmissionColor =
        Shader.PropertyToID("_EmissionColor");

    // ══════════════════════════════════════════════
    //  LIFECYCLE
    // ══════════════════════════════════════════════

    private void Awake()
    {
        _light = GetComponent<Light>();
        _urpData = GetComponent<UniversalAdditionalLightData>();

        _noiseOffset = randomizeOffset
            ? Random.Range(0f, 1000f)
            : 0f;

        float variation = speedVariation / 100f;
        _effectiveSpeed = speed * Random.Range(
            1f - variation, 1f + variation);

        _currentIntensity = Mathf.Lerp(brightnessA, brightnessB, 0.5f);

        // Инициализация MaterialPropertyBlock для Emissive
        if (emissiveTarget != null)
        {
            _emissiveMPB = new MaterialPropertyBlock();
            _currentEmissiveIntensity = Mathf.Lerp(
                emissiveIntensityMin, emissiveIntensityMax, 0.5f);
        }
    }

    private void Update()
    {
        float t = CalculateFlickerValue();

        // ---- Яркость света ----
        float minB = Mathf.Min(brightnessA, brightnessB);
        float maxB = Mathf.Max(brightnessA, brightnessB);
        _targetIntensity = Mathf.Lerp(minB, maxB, t);

        float smoothFactor = Mathf.Lerp(50f, 1f, smoothing);
        _currentIntensity = Mathf.Lerp(
            _currentIntensity,
            _targetIntensity,
            Time.deltaTime * smoothFactor
        );

        _light.intensity = _currentIntensity;

        // ---- Цвет света ----
        if (enableColorShift)
        {
            _light.color = Color.Lerp(colorA, colorB, t);
        }

        // ---- Радиус ----
        if (enableRangeFlicker)
        {
            float minR = Mathf.Min(rangeA, rangeB);
            float maxR = Mathf.Max(rangeA, rangeB);
            _light.range = Mathf.Lerp(minR, maxR, t);
        }

        // ---- Emissive материала ----
        if (enableEmissiveFlicker && emissiveTarget != null)
        {
            UpdateEmissive(t, smoothFactor);
        }
    }

    // ══════════════════════════════════════════════
    //  EMISSIVE UPDATE
    // ══════════════════════════════════════════════

    private void UpdateEmissive(float t, float smoothFactor)
    {
        if (_emissiveMPB == null)
            _emissiveMPB = new MaterialPropertyBlock();

        // Целевая яркость emission, синхронная с мерцанием
        float minE = Mathf.Min(emissiveIntensityMin, emissiveIntensityMax);
        float maxE = Mathf.Max(emissiveIntensityMin, emissiveIntensityMax);
        float targetEmissive = Mathf.Lerp(minE, maxE, t);

        // То же сглаживание, что и для света
        _currentEmissiveIntensity = Mathf.Lerp(
            _currentEmissiveIntensity,
            targetEmissive,
            Time.deltaTime * smoothFactor
        );

        // Вычисляем итоговый HDR-цвет:
        // baseColor * intensity = финальный emission color
        Color finalEmission = emissiveColor * _currentEmissiveIntensity;

        // Применяем через MaterialPropertyBlock (без копии материала)
        emissiveTarget.GetPropertyBlock(_emissiveMPB);
        _emissiveMPB.SetColor(PropEmissionColor, finalEmission);
        emissiveTarget.SetPropertyBlock(_emissiveMPB);
    }

    // ══════════════════════════════════════════════
    //  АЛГОРИТМЫ МЕРЦАНИЯ
    // ══════════════════════════════════════════════

    private float CalculateFlickerValue()
    {
        float time = Time.time * _effectiveSpeed + _noiseOffset;

        switch (mode)
        {
            case FlickerMode.Random:
                return RandomFlicker(time);
            case FlickerMode.SineWave:
                return SineFlicker(time);
            case FlickerMode.Candle:
                return CandleFlicker(time);
            case FlickerMode.Strobe:
                return StrobeFlicker(time);
            case FlickerMode.Pulse:
                return PulseFlicker(time);
            default:
                return 0.5f;
        }
    }

    private float RandomFlicker(float time)
    {
        float n1 = Mathf.PerlinNoise(time, 0f);
        float n2 = Mathf.PerlinNoise(time * 2.7f, 100f);
        return Mathf.Clamp01(n1 * 0.7f + n2 * 0.3f);
    }

    private float SineFlicker(float time)
    {
        return (Mathf.Sin(time * Mathf.PI * 2f) + 1f) * 0.5f;
    }

    private float CandleFlicker(float time)
    {
        float n1 = Mathf.PerlinNoise(time * 1.0f, _noiseOffset);
        float n2 = Mathf.PerlinNoise(time * 2.3f, _noiseOffset + 50f);
        float n3 = Mathf.PerlinNoise(time * 5.7f, _noiseOffset + 100f);

        float result = n1 * 0.5f + n2 * 0.3f + n3 * 0.2f;

        float gust = Mathf.PerlinNoise(time * 0.3f, _noiseOffset + 200f);
        if (gust < 0.3f)
            result *= Mathf.Lerp(0.3f, 1f, gust / 0.3f);

        return Mathf.Clamp01(result);
    }

    private float StrobeFlicker(float time)
    {
        float phase = (time % 1f);
        return phase < strobeDuty ? 1f : 0f;
    }

    private float PulseFlicker(float time)
    {
        float t = (Mathf.Sin(time * Mathf.PI * 2f) + 1f) * 0.5f;
        return t * t * (3f - 2f * t);
    }

    // ══════════════════════════════════════════════
    //  PUBLIC API
    // ══════════════════════════════════════════════

    public void SetBrightnessRange(float a, float b)
    {
        brightnessA = a;
        brightnessB = b;
    }

    public void SetMode(FlickerMode newMode)
    {
        mode = newMode;
    }

    public void SetEnabled(bool enabled)
    {
        this.enabled = enabled;
        if (!enabled)
        {
            _light.intensity = Mathf.Lerp(
                brightnessA, brightnessB, 0.5f);

            // При выключении — зафиксировать emission
            if (emissiveTarget != null && _emissiveMPB != null)
            {
                float midEmissive = Mathf.Lerp(
                    emissiveIntensityMin, emissiveIntensityMax, 0.5f);
                Color midColor = emissiveColor * midEmissive;
                emissiveTarget.GetPropertyBlock(_emissiveMPB);
                _emissiveMPB.SetColor(PropEmissionColor, midColor);
                emissiveTarget.SetPropertyBlock(_emissiveMPB);
            }
        }
    }

    /// <summary>
    /// Установить целевой Renderer для Emissive в рантайме.
    /// </summary>
    public void SetEmissiveTarget(Renderer target)
    {
        emissiveTarget = target;
        if (target != null)
        {
            _emissiveMPB = new MaterialPropertyBlock();
            _currentEmissiveIntensity = Mathf.Lerp(
                emissiveIntensityMin, emissiveIntensityMax, 0.5f);
        }
    }

    /// <summary>
    /// Установить параметры Emissive в рантайме.
    /// </summary>
    public void SetEmissiveRange(Color color, float min, float max)
    {
        emissiveColor = color;
        emissiveIntensityMin = min;
        emissiveIntensityMax = max;
    }
}