using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Продвинутый контроллер группы нестабильных объектов.
/// Поддерживает множество паттернов и визуальную обратную связь.
/// </summary>
public class CollapsibleGroupController : MonoBehaviour
{
    [Header("Объекты группы")]
    [SerializeField] private List<Collapsible> collapsibles = new();

    public List<Collapsible> Collapsibles => collapsibles;

    [Header("Паттерн нестабильности")]
    [Tooltip("Как объекты в группе переключаются")]
    public InstabilityPattern pattern = InstabilityPattern.Synchronized;

    [Header("Timing")]
    [Tooltip("Интервал между циклами переключения")]
    [Range(0.5f, 30f)]
    public float switchStateInterval = 3f;

    [Tooltip("Задержка между объектами (для Sequential, Wave, Domino)")]
    [Range(0f, 3f)]
    public float delayBetweenObjects = 0.3f;

    [Header("Accelerating")]
    [Tooltip("Минимальный интервал")]
    [Range(0.3f, 5f)]
    public float minInterval = 0.5f;

    [Tooltip("Скорость ускорения (секунд уменьшения за цикл)")]
    [Range(0.01f, 0.5f)]
    public float accelerationRate = 0.05f;

    [Header("Random")]
    [Tooltip("Разброс интервала (±)")]
    [Range(0f, 5f)]
    public float randomIntervalVariance = 1f;

    [Header("Clustered")]
    [Tooltip("Размер кластера (сколько объектов переключается одновременно)")]
    [Range(1, 10)]
    public int clusterSize = 2;

    [Header("Custom Pattern")]
    [Tooltip("Пользовательская последовательность индексов объектов")]
    public List<int> customSequence = new();

    [Header("Визуальная обратная связь")]
    [Tooltip("Показывать предупреждающий эффект перед переключением")]
    public bool showWarningEffect = true;

    [Tooltip("Время предупреждения до переключения (секунды)")]
    [Range(0f, 2f)]
    public float warningTime = 0.5f;

    [Tooltip("Цвет предупреждающего свечения")]
    public Color warningColor = new Color(1f, 0.3f, 0.3f, 0.8f);

    [Header("Звуковая обратная связь")]
    [Tooltip("Звук при переключении группы")]
    public AudioClip collapseSound;

    [Tooltip("Звук предупреждения")]
    public AudioClip warningSound;

    [Tooltip("Источник звука (если не назначен, создастся автоматически)")]
    public AudioSource audioSource;

    [Header("События")]
    public UnityEvent OnCycleStart;
    public UnityEvent OnCycleComplete;
    public UnityEvent<float> OnIntervalChanged; // Для Accelerating паттерна

    [Header("Состояние (только для чтения)")]
    [SerializeField] private bool _isActive = false;
    [SerializeField] private float _currentInterval;
    [SerializeField] private float _timeSinceLastSwitch;

    public float TimeSinceLastSwitch => _timeSinceLastSwitch;
    public bool IsActive => _isActive;

    private Coroutine _dynamicStateSwitchingCoroutine;
    private float _timeToDissolve;

    // Для PingPong
    private bool _pingPongForward = true;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.5f; // Частично 3D
        }
    }

    private void Start()
    {
        if (collapsibles.Count == 0)
        {
            GameDebug.LogWarning($"{gameObject.name}: CollapseGroup содержит 0 элементов");
            return;
        }

        var dissolvable = collapsibles[0].stateNew?.GetComponent<Dissolvable>();
        _timeToDissolve = dissolvable != null ? dissolvable.timeToDissolve : 0.5f;
        _currentInterval = switchStateInterval;
    }

    #region Public API

    public void StartDynamicStateSwitching()
    {
        if (_dynamicStateSwitchingCoroutine != null)
        {
            StopCoroutine(_dynamicStateSwitchingCoroutine);
        }

        _dynamicStateSwitchingCoroutine = StartCoroutine(DynamicStateSwitching());
        _isActive = true;
    }

    public void StopDynamicStateSwitching()
    {
        if (_dynamicStateSwitchingCoroutine != null)
        {
            StopCoroutine(_dynamicStateSwitchingCoroutine);
            _dynamicStateSwitchingCoroutine = null;
        }

        _isActive = false;
    }

    /// <summary>
    /// Немедленно выполнить один цикл переключения.
    /// </summary>
    public void TriggerCycleNow()
    {
        StartCoroutine(ExecutePattern());
    }

    /// <summary>
    /// Сбросить все объекты в начальное состояние.
    /// </summary>
    public void ResetAllToInitial()
    {
        foreach (var c in collapsibles)
        {
            c.Reset();
        }
    }

    #endregion

    #region Main Loop

    private IEnumerator DynamicStateSwitching()
    {
        _currentInterval = switchStateInterval;

        while (true)
        {
            _timeSinceLastSwitch = 0f;
            OnCycleStart?.Invoke();

            // Отключаем подсветку
            foreach (var collapsible in GetDynamicCollapsibles())
            {
                collapsible.stateNew?.OnUnhighlight();
                collapsible.stateOld?.OnUnhighlight();
            }

            // Предупреждение
            if (showWarningEffect && warningTime > 0f)
            {
                yield return StartCoroutine(ShowWarning());
            }

            // Выполняем переключение по паттерну
            yield return StartCoroutine(ExecutePattern());

            OnCycleComplete?.Invoke();

            // Ускорение для Accelerating паттерна
            if (pattern == InstabilityPattern.Accelerating)
            {
                _currentInterval = Mathf.Max(minInterval, _currentInterval - accelerationRate);
                OnIntervalChanged?.Invoke(_currentInterval);
            }

            // Ожидание до следующего цикла
            float waitTime = pattern == InstabilityPattern.Random
                ? _currentInterval + Random.Range(-randomIntervalVariance, randomIntervalVariance)
                : _currentInterval;

            waitTime = Mathf.Max(0.1f, waitTime);

            while (_timeSinceLastSwitch < waitTime)
            {
                _timeSinceLastSwitch += Time.deltaTime;
                yield return null;
            }
        }
    }

    private IEnumerator ShowWarning()
    {
        var dynamicCollapsibles = GetDynamicCollapsibles();

        // Включаем предупреждающее свечение
        foreach (var c in dynamicCollapsibles)
        {
            var activeState = c.CurrentState == CollapseState.Old ? c.stateOld : c.stateNew;
            if (activeState != null)
            {
                activeState.SetOutlineColor(warningColor);
                activeState.SetOutlineActive(true);
            }
        }

        // Звук предупреждения
        if (warningSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(warningSound);
        }

        yield return new WaitForSeconds(warningTime);

        // Выключаем подсветку
        foreach (var c in dynamicCollapsibles)
        {
            c.stateNew?.OnUnhighlight();
            c.stateOld?.OnUnhighlight();
        }
    }

    #endregion

    #region Patterns

    private IEnumerator ExecutePattern()
    {
        var dynamicCollapsibles = GetDynamicCollapsibles();

        switch (pattern)
        {
            case InstabilityPattern.Synchronized:
                yield return StartCoroutine(PatternSynchronized(dynamicCollapsibles));
                break;

            case InstabilityPattern.Sequential:
                yield return StartCoroutine(PatternSequential(dynamicCollapsibles));
                break;

            case InstabilityPattern.Random:
                yield return StartCoroutine(PatternRandom(dynamicCollapsibles));
                break;

            case InstabilityPattern.Wave:
                yield return StartCoroutine(PatternWave(dynamicCollapsibles));
                break;

            case InstabilityPattern.Accelerating:
                yield return StartCoroutine(PatternSynchronized(dynamicCollapsibles));
                break;

            case InstabilityPattern.PingPong:
                yield return StartCoroutine(PatternPingPong(dynamicCollapsibles));
                break;

            case InstabilityPattern.Radial:
                yield return StartCoroutine(PatternRadial(dynamicCollapsibles));
                break;

            case InstabilityPattern.Clustered:
                yield return StartCoroutine(PatternClustered(dynamicCollapsibles));
                break;

            case InstabilityPattern.Domino:
                yield return StartCoroutine(PatternDomino(dynamicCollapsibles));
                break;

            case InstabilityPattern.Custom:
                yield return StartCoroutine(PatternCustom(dynamicCollapsibles));
                break;
        }

        // Звук после завершения паттерна
        if (collapseSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(collapseSound);
        }
    }

    /// <summary>Все одновременно</summary>
    private IEnumerator PatternSynchronized(List<Collapsible> targets)
    {
        yield return new WaitForSeconds(_timeToDissolve * 2 + 0.1f);

        foreach (var c in targets)
        {
            c.CollapseByTimer();
        }
    }

    /// <summary>По очереди</summary>
    private IEnumerator PatternSequential(List<Collapsible> targets)
    {
        foreach (var c in targets)
        {
            yield return new WaitForSeconds(_timeToDissolve * 2 + 0.1f);
            c.CollapseByTimer();
            yield return new WaitForSeconds(delayBetweenObjects);
        }
    }

    /// <summary>Случайный порядок</summary>
    private IEnumerator PatternRandom(List<Collapsible> targets)
    {
        var shuffled = targets.OrderBy(_ => Random.value).ToList();

        yield return new WaitForSeconds(_timeToDissolve * 2 + 0.1f);

        foreach (var c in shuffled)
        {
            c.CollapseByTimer();
            yield return new WaitForSeconds(Random.Range(0.05f, delayBetweenObjects));
        }
    }

    /// <summary>Волна от начала к концу</summary>
    private IEnumerator PatternWave(List<Collapsible> targets)
    {
        yield return new WaitForSeconds(_timeToDissolve * 2 + 0.1f);

        for (int i = 0; i < targets.Count; i++)
        {
            targets[i].CollapseByTimer();
            if (i < targets.Count - 1)
            {
                yield return new WaitForSeconds(delayBetweenObjects);
            }
        }
    }

    /// <summary>Туда-обратно</summary>
    private IEnumerator PatternPingPong(List<Collapsible> targets)
    {
        yield return new WaitForSeconds(_timeToDissolve * 2 + 0.1f);

        if (_pingPongForward)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].CollapseByTimer();
                if (i < targets.Count - 1)
                    yield return new WaitForSeconds(delayBetweenObjects);
            }
        }
        else
        {
            for (int i = targets.Count - 1; i >= 0; i--)
            {
                targets[i].CollapseByTimer();
                if (i > 0)
                    yield return new WaitForSeconds(delayBetweenObjects);
            }
        }

        _pingPongForward = !_pingPongForward;
    }

    /// <summary>От центра к краям</summary>
    private IEnumerator PatternRadial(List<Collapsible> targets)
    {
        yield return new WaitForSeconds(_timeToDissolve * 2 + 0.1f);

        // Сортируем по расстоянию от центра группы
        Vector3 center = transform.position;
        var sorted = targets.OrderBy(c => Vector3.Distance(c.transform.position, center)).ToList();

        foreach (var c in sorted)
        {
            c.CollapseByTimer();
            yield return new WaitForSeconds(delayBetweenObjects);
        }
    }

    /// <summary>Группами (кластерами)</summary>
    private IEnumerator PatternClustered(List<Collapsible> targets)
    {
        yield return new WaitForSeconds(_timeToDissolve * 2 + 0.1f);

        int clusterCount = Mathf.CeilToInt((float)targets.Count / clusterSize);

        for (int cluster = 0; cluster < clusterCount; cluster++)
        {
            int startIndex = cluster * clusterSize;
            int endIndex = Mathf.Min(startIndex + clusterSize, targets.Count);

            // Переключаем все объекты в кластере одновременно
            for (int i = startIndex; i < endIndex; i++)
            {
                targets[i].CollapseByTimer();
            }

            if (cluster < clusterCount - 1)
            {
                yield return new WaitForSeconds(delayBetweenObjects);
            }
        }
    }

    /// <summary>Домино с ускорением</summary>
    private IEnumerator PatternDomino(List<Collapsible> targets)
    {
        yield return new WaitForSeconds(_timeToDissolve * 2 + 0.1f);

        float currentDelay = delayBetweenObjects;
        float acceleration = 0.9f; // Множитель ускорения

        for (int i = 0; i < targets.Count; i++)
        {
            targets[i].CollapseByTimer();

            if (i < targets.Count - 1)
            {
                yield return new WaitForSeconds(currentDelay);
                currentDelay = Mathf.Max(0.05f, currentDelay * acceleration);
            }
        }
    }

    /// <summary>Пользовательская последовательность</summary>
    private IEnumerator PatternCustom(List<Collapsible> targets)
    {
        yield return new WaitForSeconds(_timeToDissolve * 2 + 0.1f);

        if (customSequence == null || customSequence.Count == 0)
        {
            GameDebug.LogWarning($"{name}: Custom sequence is empty! Falling back to Sequential.");
            yield return StartCoroutine(PatternSequential(targets));
            yield break;
        }

        foreach (int index in customSequence)
        {
            if (index >= 0 && index < targets.Count)
            {
                targets[index].CollapseByTimer();
                yield return new WaitForSeconds(delayBetweenObjects);
            }
            else
            {
                GameDebug.LogWarning($"{name}: Custom sequence index {index} out of range!");
            }
        }
    }

    #endregion

    #region Helpers

    private List<Collapsible> GetDynamicCollapsibles()
    {
        return collapsibles.Where(c => c != null && c.IsDynamic).ToList();
    }

    #endregion

    #region Editor

    public void SetCollapsiblesFromChildren()
    {
        collapsibles = GetComponentsInChildren<Collapsible>().ToList();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (collapsibles == null || collapsibles.Count < 2) return;

        bool isSelected = UnityEditor.Selection.activeGameObject == gameObject;
        Color lineColor = pattern switch
        {
            InstabilityPattern.Synchronized => new Color(1f, 0.7f, 0f, 0.6f),
            InstabilityPattern.Wave => new Color(0.3f, 0.7f, 1f, 0.6f),
            InstabilityPattern.Random => new Color(0.8f, 0.3f, 0.8f, 0.6f),
            InstabilityPattern.Accelerating => new Color(1f, 0.3f, 0.3f, 0.6f),
            InstabilityPattern.PingPong => new Color(0.3f, 1f, 0.7f, 0.6f),
            InstabilityPattern.Radial => new Color(1f, 1f, 0.3f, 0.6f),
            InstabilityPattern.Clustered => new Color(0.5f, 0.5f, 1f, 0.6f),
            InstabilityPattern.Domino => new Color(1f, 0.5f, 0.2f, 0.6f),
            _ => new Color(1f, 0.5f, 0f, 0.5f)
        };

        // Линии между объектами
        for (int i = 0; i < collapsibles.Count; i++)
        {
            if (collapsibles[i] == null) continue;
            var startPos = collapsibles[i].transform.position;

            for (int j = i + 1; j < collapsibles.Count; j++)
            {
                if (collapsibles[j] == null) continue;
                var endPos = collapsibles[j].transform.position;

                if (isSelected)
                {
                    UnityEditor.Handles.color = lineColor;
                    UnityEditor.Handles.DrawLine(startPos, endPos, 3f);
                }
                else
                {
                    Gizmos.color = lineColor;
                    Gizmos.DrawLine(startPos, endPos);
                }
            }

            // Сферы на объектах
            if (isSelected)
            {
                UnityEditor.Handles.color = lineColor;
                UnityEditor.Handles.SphereHandleCap(0, startPos, Quaternion.identity, 0.2f, EventType.Repaint);
            }
            else
            {
                Gizmos.color = lineColor;
                Gizmos.DrawWireSphere(startPos, 0.2f);
            }
        }

        // Подпись
        if (isSelected)
        {
            string info = $"⚡ {pattern}\nInterval: {switchStateInterval:F1}s";
            if (pattern == InstabilityPattern.Accelerating)
                info += $"\nMin: {minInterval:F1}s";
            if (pattern == InstabilityPattern.Clustered)
                info += $"\nCluster: {clusterSize}";

            GUIStyle style = new GUIStyle
            {
                normal = { textColor = lineColor },
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1f, info, style);
        }
    }
#endif

    #endregion
}