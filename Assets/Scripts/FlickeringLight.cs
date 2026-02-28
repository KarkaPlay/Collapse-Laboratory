using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Flickering Light — компонент мерцающего света для Unity 6 URP.
/// Поддерживает несколько режимов мерцания, два значения яркости,
/// опциональное изменение цвета и радиуса.
/// </summary>
[RequireComponent(typeof(Light))]
public class FlickeringLight : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  ENUMS
    // ──────────────────────────────────────────────

    public enum FlickerMode
    {
        Random,     // случайные скачки между двумя значениями
        SineWave,   // плавная синусоида
        Candle,     // имитация пламени свечи (Perlin noise)
        Strobe,     // резкое вкл/выкл
        Pulse       // плавный пульс (ease-in-out)
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
    public Color colorA = new Color(1f, 0.85f, 0.6f); // тёплый

    [Tooltip("Второй цвет")]
    public Color colorB = new Color(1f, 0.5f, 0.2f);  // оранжевый

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
    //  STROBE — специальные настройки
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

    // ══════════════════════════════════════════════
    //  LIFECYCLE
    // ══════════════════════════════════════════════

    private void Awake()
    {
        _light = GetComponent<Light>();
        _urpData = GetComponent<UniversalAdditionalLightData>();

        // Случайный сдвиг, чтобы одинаковые источники не мерцали синхронно
        _noiseOffset = randomizeOffset
            ? Random.Range(0f, 1000f)
            : 0f;

        // Вариация скорости
        float variation = speedVariation / 100f;
        _effectiveSpeed = speed * Random.Range(1f - variation, 1f + variation);

        _currentIntensity = Mathf.Lerp(brightnessA, brightnessB, 0.5f);
    }

    private void Update()
    {
        float t = CalculateFlickerValue();

        // Целевая яркость
        float minB = Mathf.Min(brightnessA, brightnessB);
        float maxB = Mathf.Max(brightnessA, brightnessB);
        _targetIntensity = Mathf.Lerp(minB, maxB, t);

        // Сглаживание
        float smoothFactor = Mathf.Lerp(50f, 1f, smoothing);
        _currentIntensity = Mathf.Lerp(
            _currentIntensity,
            _targetIntensity,
            Time.deltaTime * smoothFactor
        );

        // Применяем яркость
        _light.intensity = _currentIntensity;

        // Цвет
        if (enableColorShift)
        {
            _light.color = Color.Lerp(colorA, colorB, t);
        }

        // Радиус
        if (enableRangeFlicker)
        {
            float minR = Mathf.Min(rangeA, rangeB);
            float maxR = Mathf.Max(rangeA, rangeB);
            _light.range = Mathf.Lerp(minR, maxR, t);
        }
    }

    // ══════════════════════════════════════════════
    //  АЛГОРИТМЫ МЕРЦАНИЯ
    // ══════════════════════════════════════════════

    /// <summary>
    /// Возвращает значение 0..1, определяющее текущую «фазу» мерцания.
    /// </summary>
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

    // --- Random ---
    private float RandomFlicker(float time)
    {
        // Два слоя Perlin noise для более интересного результата
        float n1 = Mathf.PerlinNoise(time, 0f);
        float n2 = Mathf.PerlinNoise(time * 2.7f, 100f);
        return Mathf.Clamp01(n1 * 0.7f + n2 * 0.3f);
    }

    // --- Sine Wave ---
    private float SineFlicker(float time)
    {
        return (Mathf.Sin(time * Mathf.PI * 2f) + 1f) * 0.5f;
    }

    // --- Candle ---
    private float CandleFlicker(float time)
    {
        // Многослойный Perlin noise имитирует пламя
        float n1 = Mathf.PerlinNoise(time * 1.0f, _noiseOffset);
        float n2 = Mathf.PerlinNoise(time * 2.3f, _noiseOffset + 50f);
        float n3 = Mathf.PerlinNoise(time * 5.7f, _noiseOffset + 100f);

        float result = n1 * 0.5f + n2 * 0.3f + n3 * 0.2f;

        // Иногда резкие провалы (имитация порыва ветра)
        float gust = Mathf.PerlinNoise(time * 0.3f, _noiseOffset + 200f);
        if (gust < 0.3f)
            result *= Mathf.Lerp(0.3f, 1f, gust / 0.3f);

        return Mathf.Clamp01(result);
    }

    // --- Strobe ---
    private float StrobeFlicker(float time)
    {
        float phase = (time % 1f);
        return phase < strobeDuty ? 1f : 0f;
    }

    // --- Pulse (ease-in-out) ---
    private float PulseFlicker(float time)
    {
        float t = (Mathf.Sin(time * Mathf.PI * 2f) + 1f) * 0.5f;
        // Smooth-step для более выраженного ease-in-out
        return t * t * (3f - 2f * t);
    }

    // ══════════════════════════════════════════════
    //  PUBLIC API
    // ══════════════════════════════════════════════

    /// <summary>
    /// Установить новый диапазон яркости в рантайме.
    /// </summary>
    public void SetBrightnessRange(float a, float b)
    {
        brightnessA = a;
        brightnessB = b;
    }

    /// <summary>
    /// Сменить режим мерцания в рантайме.
    /// </summary>
    public void SetMode(FlickerMode newMode)
    {
        mode = newMode;
    }

    /// <summary>
    /// Включить / выключить мерцание. При выключении яркость = среднее.
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        this.enabled = enabled;
        if (!enabled)
        {
            _light.intensity = Mathf.Lerp(brightnessA, brightnessB, 0.5f);
        }
    }
}