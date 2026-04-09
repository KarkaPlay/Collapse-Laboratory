using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Базовый класс для всех объектов, поддерживающих схлопывание.
/// Центральный элемент системы — переключает объект между двумя временными состояниями.
/// </summary>
public class Collapsible : MonoBehaviour
{
    [Header("Объекты состояний")]
    [Tooltip("Состояние 'прошлое' (1980-е, функционирующее)")]
    public COState stateOld;

    [Tooltip("Состояние 'настоящее' (2030-е, разрушенное)")]
    public COState stateNew;

    [Header("Настройки")]
    [Tooltip("Начальное состояние при загрузке сцены")]
    public CollapseState initialState = CollapseState.Old;

    [Tooltip("Уровень стабильности — определяет правила взаимодействия")]
    public StabilityLevel stabilityLevel = StabilityLevel.Weak;

    [Header("Состояние (только для чтения)")]
    [SerializeField] private CollapseState _currentState = CollapseState.Old;
    public CollapseState CurrentState => _currentState;

    [Header("События")]
    [Tooltip("Вызывается при каждом схлопывании с полными данными")]
    public UnityEvent<CollapseEventData> OnCollapse;

    [Tooltip("Вызывается при переходе в Old")]
    public UnityEvent OnCollapseToOld;

    [Tooltip("Вызывается при переходе в New")]
    public UnityEvent OnCollapseToNew;

    // === Вычисляемые свойства на основе StabilityLevel ===

    /// <summary>Может ли игрок схлопнуть этот объект напрямую</summary>
    public bool CanPlayerCollapse =>
        stabilityLevel == StabilityLevel.Weak ||
        stabilityLevel == StabilityLevel.Unstable;

    /// <summary>Переключается ли объект по таймеру</summary>
    public bool IsDynamic => stabilityLevel == StabilityLevel.Unstable;

    /// <summary>Может ли быть целью запутанности (связи)</summary>
    public bool CanBeLinkedTarget =>
        stabilityLevel == StabilityLevel.Strong ||
        stabilityLevel == StabilityLevel.Weak ||
        stabilityLevel == StabilityLevel.Unstable;

    /// <summary>Можно ли вообще изменить состояние объекта</summary>
    public bool CanBeChanged => stabilityLevel != StabilityLevel.Absolute;

    // === Обратная совместимость (deprecated, но работает) ===

    [Obsolete("Используйте stabilityLevel вместо isDynamic")]
    public bool isDynamic => IsDynamic;

    [Obsolete("Используйте stabilityLevel вместо canPlayerCollapse")]
    public bool canPlayerCollapse => CanPlayerCollapse;

    [Obsolete("Используйте stabilityLevel")]
    public bool isBroken => stabilityLevel == StabilityLevel.Absolute;

    private void Awake()
    {
        _currentState = initialState;
    }

    private void Start()
    {
        SetObjectsActive();
    }

    #region Схлопывание — основные методы

    /// <summary>
    /// Главный метод схлопывания. Все остальные методы вызывают его.
    /// </summary>
    /// <param name="origin">Кто инициировал</param>
    /// <param name="targetState">Целевое состояние (null = toggle)</param>
    /// <returns>true если схлопывание произошло</returns>
    public bool Collapse(CollapseOrigin origin, CollapseState? targetState = null)
    {
        // Проверка: можно ли вообще менять этот объект
        if (!CanBeChanged)
        {
            GameDebug.Log($"[Collapsible] {name}: Absolute stability — cannot collapse");
            return false;
        }

        // Проверка: может ли игрок менять напрямую
        if (origin == CollapseOrigin.Player && !CanPlayerCollapse)
        {
            GameDebug.Log($"[Collapsible] {name}: Player cannot collapse (stability: {stabilityLevel})");
            // TODO: звук/эффект "не могу"
            return false;
        }

        // Проверка: Strong объекты можно менять только через Chain
        if (stabilityLevel == StabilityLevel.Strong && origin != CollapseOrigin.Chain && origin != CollapseOrigin.Script)
        {
            GameDebug.Log($"[Collapsible] {name}: Strong stability — only chain/script can collapse");
            return false;
        }

        // Определяем новое состояние
        CollapseState previousState = _currentState;
        CollapseState newState;

        if (targetState.HasValue)
        {
            newState = targetState.Value;
            if (newState == _currentState)
            {
                // Уже в нужном состоянии
                return false;
            }
        }
        else
        {
            // Toggle
            newState = _currentState == CollapseState.Old ? CollapseState.New : CollapseState.Old;
        }

        // Выполняем переключение
        _currentState = newState;
        SetObjectsActive();

        // Создаём данные события
        var eventData = new CollapseEventData(this, previousState, newState, origin);
        GameDebug.Log($"[Collapsible] {eventData}");

        // Вызываем события
        OnCollapse?.Invoke(eventData);

        if (newState == CollapseState.Old)
            OnCollapseToOld?.Invoke();
        else
            OnCollapseToNew?.Invoke();

        return true;
    }

    /// <summary>Схлопывание игроком (toggle)</summary>
    public bool CollapseByPlayer()
    {
        return Collapse(CollapseOrigin.Player);
    }

    /// <summary>Схлопывание по таймеру (toggle)</summary>
    public bool CollapseByTimer()
    {
        return Collapse(CollapseOrigin.Timer);
    }

    /// <summary>Схлопывание через цепочку</summary>
    public bool CollapseByChain(CollapseState? targetState = null)
    {
        return Collapse(CollapseOrigin.Chain, targetState);
    }

    /// <summary>Схлопывание из скрипта / Unity Event</summary>
    public bool CollapseByScript(CollapseState? targetState = null)
    {
        return Collapse(CollapseOrigin.Script, targetState);
    }

    // Удобные методы для Unity Events (не принимают параметров)
    public void CollapseToggle() => Collapse(CollapseOrigin.Script);
    public void CollapseToOld() => Collapse(CollapseOrigin.Script, CollapseState.Old);
    public void CollapseToNew() => Collapse(CollapseOrigin.Script, CollapseState.New);

    /// <summary>Сбросить в начальное состояние</summary>
    public void Reset()
    {
        _currentState = initialState;
        SetObjectsActive();
    }

    #endregion

    #region Внутренние методы

    private void SetObjectsActive()
    {
        if (stateNew != null) stateNew.Activate(_currentState == CollapseState.New);
        if (stateOld != null) stateOld.Activate(_currentState == CollapseState.Old);
    }

    #endregion

    #region Информация для дизайнеров

    /// <summary>
    /// Человекочитаемое описание объекта для дебаг-инструментов.
    /// </summary>
    public string GetDesignerDescription()
    {
        string stability = stabilityLevel switch
        {
            StabilityLevel.Absolute => "🔒 Абсолютный — нельзя изменить",
            StabilityLevel.Strong => "🔗 Сильный — только через связь",
            StabilityLevel.Weak => "✋ Слабый — игрок может менять",
            StabilityLevel.Unstable => "⚡ Нестабильный — меняется сам",
            _ => "?"
        };

        string state = _currentState == CollapseState.Old ? "Прошлое (Old)" : "Настоящее (New)";

        return $"{name}\n{stability}\nСостояние: {state}";
    }

    /// <summary>
    /// Цвет для визуализации в зависимости от уровня стабильности.
    /// </summary>
    public Color GetStabilityColor()
    {
        return stabilityLevel switch
        {
            StabilityLevel.Absolute => new Color(0.5f, 0.5f, 0.5f),  // Серый
            StabilityLevel.Strong => new Color(1f, 0.8f, 0f),         // Жёлтый
            StabilityLevel.Weak => new Color(0.3f, 0.6f, 1f),         // Синий
            StabilityLevel.Unstable => new Color(1f, 0.2f, 0.2f),     // Красный
            _ => Color.white
        };
    }

    #endregion

    #region Editor

    public void SetCOStatesFromChildren()
    {
        var old = transform.Find($"{gameObject.name}_OLD");
        var newState = transform.Find($"{gameObject.name}_NEW");

        if (old == null || newState == null)
        {
            Debug.LogError(
                $"COState {gameObject.name}_OLD или {gameObject.name}_NEW не найдены. Проверьте названия дочерних объектов");
            return;
        }

        stateOld = old.GetComponent<COState>();
        stateNew = newState.GetComponent<COState>();
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Цвет по стабильности
        Gizmos.color = GetStabilityColor();
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.3f);

        // Для нестабильных — пульсирующий эффект
        if (stabilityLevel == StabilityLevel.Unstable)
        {
            float pulse = Mathf.Sin(Time.realtimeSinceStartup * 3f) * 0.5f + 0.5f;
            Color c = GetStabilityColor();
            c.a = pulse * 0.5f;
            Gizmos.color = c;
            Gizmos.DrawCube(transform.position, Vector3.one * 0.35f);
        }

        // Для Dynamic — таймер
        var groupController = GetComponentInParent<CollapsibleGroupController>();
        if (IsDynamic && groupController != null)
        {
            float timeRemaining =
                Mathf.Max(0, groupController.switchStateInterval - groupController.TimeSinceLastSwitch);
            string timeText = $"{stabilityLevel} | Next: {timeRemaining:F1}s";

            Vector3 position = transform.position + Vector3.up * 0.5f;

            GUIStyle style = new GUIStyle();
            style.normal.textColor = GetStabilityColor();
            style.fontSize = 11;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;

            UnityEditor.Handles.Label(position, timeText, style);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // При выделении — показать более подробную информацию
        Vector3 position = transform.position + Vector3.up * 1f;

        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.fontSize = 12;
        style.alignment = TextAnchor.MiddleCenter;
        style.normal.background = Texture2D.grayTexture;
        style.padding = new RectOffset(5, 5, 3, 3);

        string info = $"[{stabilityLevel}] {_currentState}\n" +
                      $"Player: {(CanPlayerCollapse ? "✓" : "✗")} | " +
                      $"Chain: {(CanBeLinkedTarget ? "✓" : "✗")} | " +
                      $"Timer: {(IsDynamic ? "✓" : "✗")}";

        UnityEditor.Handles.Label(position, info, style);
    }
#endif

    #endregion
}