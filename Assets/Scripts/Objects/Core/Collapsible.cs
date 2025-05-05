using System.Collections;
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
    private CollapseState currentState = CollapseState.Old;
    public CollapseState CurrentState { get => currentState; }

    [Header("Будет ли периодически менять состояние")]
    public bool isDynamic;

    [Header("Может ли игрок схлопывать")]
    public bool canPlayerCollapse = true;

    [Header("Объект сломан")]
    public bool isBroken = false;

    public UnityEvent<Collapsible> OnCollapse;

    private void Awake()
    {
        currentState = initialState;
    }

    void Start()
    {
        SetObjectsActive();
    }

    public void SetDynamic(bool newState) => isDynamic = newState;

    public void SetIsBroken(bool newState) => isBroken = newState;

    public void SetCanPlayerCollapse(bool newState) => canPlayerCollapse = newState;

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

    #region Схлопывание
    public void Collapse(bool byPlayer = false)
    {
        if (byPlayer && !canPlayerCollapse)
        {
            Debug.Log($"Игрок не может схлопнуть {gameObject.name}");
            // TODO: Добавить эффект или звук
            return;
        }

        currentState = currentState == CollapseState.Old ? CollapseState.New : CollapseState.Old;
        SetObjectsActive();
        OnCollapse?.Invoke(this);
    }

    public void Collapse(CollapseState toState)
    {
        currentState = toState;
        SetObjectsActive();
    }

    public void Collapse(int toState)
    {
        Collapse((CollapseState)toState);
    }

    public void Reset()
    {
        currentState = initialState;
        SetObjectsActive();
    }

    private void SetObjectsActive()
    {
        stateNew.Activate(currentState == CollapseState.New);
        stateOld.Activate(currentState == CollapseState.Old);
    }
    #endregion
}