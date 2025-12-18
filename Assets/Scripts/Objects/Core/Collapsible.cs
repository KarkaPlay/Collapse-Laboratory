using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

// Базовый класс для всех объектов, поддерживающих схлопывание
public class Collapsible : MonoBehaviour
{
    [Header("Объекты состояний")]
    public COState stateOld;
    public COState stateNew;

    [Header("Начальное состояние")]
    public CollapseState initialState = CollapseState.Old;
    private CollapseState _currentState = CollapseState.Old;
    public CollapseState CurrentState => _currentState;

    [Header("Будет ли периодически менять состояние")]
    public bool isDynamic;

    [Header("Может ли игрок схлопывать")]
    public bool canPlayerCollapse = true;

    [Header("Объект сломан")]
    public bool isBroken;

    public UnityEvent<Collapsible> OnCollapse;
    
    public CollapsibleGroupController _groupController;

    private void Awake()
    {
        _currentState = initialState;
    }

    void Start()
    {
        SetObjectsActive();
        _groupController = GetComponentInParent<CollapsibleGroupController>();
    }

    #region SetParams

    public void SetDynamic(bool newState) => isDynamic = newState;

    public void SetIsBroken(bool newState) => isBroken = newState;

    public void SetCanPlayerCollapse(bool newState) => canPlayerCollapse = newState;

    #endregion
    
    #region Editor

    public void SetCOStatesFromChildren()
    {
        var old = transform.Find($"{gameObject.name}_OLD");
        var newState = transform.Find($"{gameObject.name}_NEW");

        if (old == null || newState == null)
        {
            Debug.LogError($"COState {gameObject.name}_OLD или {gameObject.name}_NEW не найдены. Проверьте названия дочерних объектов");
            return;
        }

        stateOld = old.GetComponent<COState>();
        stateNew = newState.GetComponent<COState>();
    }
    
    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (!isDynamic || _groupController == null) return;
    
        // Рассчитываем оставшееся время
        float timeRemaining = Mathf.Max(0, _groupController.switchStateInterval - _groupController.timeSinceLastSwitch);
        string timeText = $"Next collapse: {timeRemaining:F1}s";
    
        // Позиция для отображения текста (над объектом)
        Vector3 position = transform.position + Vector3.up * 0.5f;
    
        // Стиль текста
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.yellow;
        style.fontSize = 12;
        style.fontStyle = FontStyle.Bold;
        style.alignment = TextAnchor.MiddleCenter;
    
        // Рисуем текст
        Handles.Label(position, timeText, style);
    
        // Рисуем линию к объекту
        Handles.color = Color.yellow;
        Handles.DrawDottedLine(position, transform.position, 2f);
#endif
    }
    
    #endregion

    #region Схлопывание
    public void Collapse(bool byPlayer = false, bool invokeOnCollapse = true)
    {
        if (byPlayer && !canPlayerCollapse)
        {
            Debug.Log($"Игрок не может схлопнуть {gameObject.name}");
            // TODO: Добавить эффект или звук
            return;
        }

        _currentState = _currentState == CollapseState.Old ? CollapseState.New : CollapseState.Old;
        SetObjectsActive();
        if (invokeOnCollapse)
        {
            OnCollapse?.Invoke(this);
        }
    }

    public void Collapse(CollapseState toState)
    {
        _currentState = toState;
        SetObjectsActive();
    }

    public void Collapse(int toState)
    {
        Collapse((CollapseState)toState);
    }

    public void Reset()
    {
        _currentState = initialState;
        SetObjectsActive();
    }

    private void SetObjectsActive()
    {
        stateNew.Activate(_currentState == CollapseState.New);
        stateOld.Activate(_currentState == CollapseState.Old);
    }
    #endregion
}