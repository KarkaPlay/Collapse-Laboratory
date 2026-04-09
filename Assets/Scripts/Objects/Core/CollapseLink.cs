using UnityEngine;

/// <summary>
/// Описывает одну связь (запутанность) между объектами.
/// Хранит условие срабатывания, действие и параметры.
/// </summary>
[System.Serializable]
public class CollapseLink
{
    [Tooltip("Целевой объект, на который воздействует связь")]
    public Collapsible target;

    [Tooltip("При каком условии срабатывает связь")]
    public CollapseTriggerCondition triggerWhen = CollapseTriggerCondition.OnAnyCollapse;

    [Tooltip("Какое действие выполнить над целью")]
    public CollapseLinkAction action = CollapseLinkAction.Toggle;

    [Tooltip("Задержка перед срабатыванием (секунды)")]
    [Range(0f, 5f)]
    public float delay = 0.3f;

    [Tooltip("Показывать визуальный след (trail) при срабатывании")]
    public bool showTrail = true;

    [Tooltip("Описание связи для дизайнера")]
    [TextArea(1, 3)]
    public string designerNote;

    /// <summary>
    /// Проверяет, должна ли связь сработать при данном событии.
    /// </summary>
    public bool ShouldTrigger(CollapseEventData eventData)
    {
        if (target == null) return false;
        if (!target.CanBeLinkedTarget) return false;

        return triggerWhen switch
        {
            CollapseTriggerCondition.OnAnyCollapse => true,
            CollapseTriggerCondition.OnCollapseToOld => eventData.NewState == CollapseState.Old,
            CollapseTriggerCondition.OnCollapseToNew => eventData.NewState == CollapseState.New,
            CollapseTriggerCondition.OnPlayerCollapse => eventData.Origin == CollapseOrigin.Player,
            CollapseTriggerCondition.OnChainCollapse => eventData.Origin == CollapseOrigin.Chain,
            CollapseTriggerCondition.OnTimerCollapse => eventData.Origin == CollapseOrigin.Timer,
            _ => false
        };
    }

    /// <summary>
    /// Определяет целевое состояние для действия.
    /// </summary>
    public CollapseState? GetTargetState(CollapseEventData eventData)
    {
        return action switch
        {
            CollapseLinkAction.Toggle => null, // toggle
            CollapseLinkAction.SetToOld => CollapseState.Old,
            CollapseLinkAction.SetToNew => CollapseState.New,
            CollapseLinkAction.MatchSource => eventData.NewState,
            CollapseLinkAction.InvertSource => eventData.NewState == CollapseState.Old
                ? CollapseState.New
                : CollapseState.Old,
            _ => null
        };
    }

    /// <summary>
    /// Человекочитаемое описание связи.
    /// </summary>
    public string GetDescription()
    {
        string targetName = target != null ? target.name : "(не назначен)";
        string condition = triggerWhen switch
        {
            CollapseTriggerCondition.OnAnyCollapse => "при любом схлопывании",
            CollapseTriggerCondition.OnCollapseToOld => "при переходе в Old",
            CollapseTriggerCondition.OnCollapseToNew => "при переходе в New",
            CollapseTriggerCondition.OnPlayerCollapse => "когда игрок схлопнет",
            CollapseTriggerCondition.OnChainCollapse => "при цепной реакции",
            CollapseTriggerCondition.OnTimerCollapse => "при срабатывании таймера",
            _ => "?"
        };
        string act = action switch
        {
            CollapseLinkAction.Toggle => "переключить",
            CollapseLinkAction.SetToOld => "→ Old",
            CollapseLinkAction.SetToNew => "→ New",
            CollapseLinkAction.MatchSource => "= как источник",
            CollapseLinkAction.InvertSource => "≠ противоположно источнику",
            _ => "?"
        };

        string delayText = delay > 0 ? $" (через {delay:F1}с)" : "";

        return $"{condition} → {targetName}: {act}{delayText}";
    }
}