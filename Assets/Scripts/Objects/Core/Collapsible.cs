using System.Collections;
using UnityEngine;

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

    private void Awake()
    {
        currentState = initialState;
    }

    void Start()
    {
        SetObjectsActive();
    }

    public void SetDynamic(bool newState)
    {
        isDynamic = newState;
    }

    public void SetCanPlayerCollapse(bool newState)
    {
        canPlayerCollapse = newState;
    }

    public void SetCOStatesFromChildren()
    {
        if (transform.Find($"{gameObject.name}_OLD") == null || transform.Find($"{gameObject.name}_NEW") == null)
        {
            Debug.LogError($"COState {gameObject.name}_OLD или {gameObject.name}_NEW не найдены. Проверьте названия дочерних объектов");
            return;
        }

        // Дочерние объекты названы по шаблону name_OLD и name_NEW
        stateOld = transform.Find($"{gameObject.name}_OLD").GetComponent<COState>();
        stateNew = transform.Find($"{gameObject.name}_NEW").GetComponent<COState>();
    }

    #region Схлопывание
    public void Collapse(bool byPlayer = false)
    {
        if (!byPlayer || (byPlayer && canPlayerCollapse))
        {
            currentState = currentState == CollapseState.Old ? CollapseState.New : CollapseState.Old;
            SetObjectsActive();
        }
        else
        {
            Debug.Log($"Игрок не может схлопнуть {gameObject.name}");
            // TODO: Добавить эффект или звук
        }
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
        stateNew.Acivate(currentState == CollapseState.New);
        stateOld.Acivate(currentState == CollapseState.Old);
    }
    #endregion
}