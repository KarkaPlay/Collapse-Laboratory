using UnityEngine;

// Базовый класс для всех объектов, поддерживающих схлопывание
public class Collapsible : MonoBehaviour
{
    public COState stateNew;
    public COState stateOld;

    public CollapseState initialState = CollapseState.Old;
    public CollapseState currentState = CollapseState.Old;

    public void Collapse()
    {
        currentState = currentState == CollapseState.Old ? CollapseState.New : CollapseState.Old;
        SetObjectsActive();
    }

    public void Collapse(CollapseState toState)
    {
        currentState = toState;
        SetObjectsActive();
    }

    public void Reset()
    {
        currentState = initialState;
        SetObjectsActive();
    }

    private void SetObjectsActive()
    {
        stateNew.Acivate(currentState == CollapseState.New);
        stateNew.OnCollapseUnhighlight();
        
        stateOld.Acivate(currentState == CollapseState.Old);
        stateOld.OnCollapseUnhighlight();
    }
}