/// <summary>
/// Данные о событии схлопывания.
/// Передаются всем подписчикам при переключении объекта.
/// </summary>
[System.Serializable]
public struct CollapseEventData
{
    /// <summary>Объект, который схлопнулся</summary>
    public Collapsible Source;

    /// <summary>Состояние ДО схлопывания</summary>
    public CollapseState PreviousState;

    /// <summary>Состояние ПОСЛЕ схлопывания</summary>
    public CollapseState NewState;

    /// <summary>Кто инициировал схлопывание</summary>
    public CollapseOrigin Origin;

    public CollapseEventData(Collapsible source, CollapseState previousState, CollapseState newState,
        CollapseOrigin origin)
    {
        Source = source;
        PreviousState = previousState;
        NewState = newState;
        Origin = origin;
    }

    public override string ToString()
    {
        return $"[{Source?.name}] {PreviousState} → {NewState} (by {Origin})";
    }
}