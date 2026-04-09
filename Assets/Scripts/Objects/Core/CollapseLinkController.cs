using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Контроллер связей (запутанности) между объектами.
/// Размещается на том же объекте, что и Collapsible-источник.
/// При схлопывании источника активирует связанные объекты.
/// </summary>
[RequireComponent(typeof(Collapsible))]
[RequireComponent(typeof(TrailMoving))]
public class CollapseLinkController : MonoBehaviour
{
    [Header("Связи (запутанности)")]
    [Tooltip("Список связей с другими объектами")]
    public List<CollapseLink> links = new();

    [Header("Настройки")]
    [Tooltip("Защита от бесконечных цепочек")]
    [Range(1, 10)]
    public int maxChainDepth = 5;

    private Collapsible _collapsible;
    private TrailMoving _trailMoving;

    // Статический счётчик для защиты от бесконечных цепочек
    private static int _currentChainDepth = 0;

    private void Awake()
    {
        _collapsible = GetComponent<Collapsible>();
        _trailMoving = GetComponent<TrailMoving>();
    }

    private void OnEnable()
    {
        _collapsible.OnCollapse.AddListener(OnSourceCollapsed);
    }

    private void OnDisable()
    {
        _collapsible.OnCollapse.RemoveListener(OnSourceCollapsed);
    }

    /// <summary>
    /// Вызывается когда источник (этот объект) схлопывается.
    /// </summary>
    private void OnSourceCollapsed(CollapseEventData eventData)
    {
        foreach (var link in links)
        {
            if (link.ShouldTrigger(eventData))
            {
                if (link.delay > 0)
                {
                    StartCoroutine(ExecuteLinkWithDelay(link, eventData));
                }
                else
                {
                    ExecuteLink(link, eventData);
                }
            }
        }
    }

    private IEnumerator ExecuteLinkWithDelay(CollapseLink link, CollapseEventData eventData)
    {
        // Запускаем визуальный след
        if (link.showTrail && _trailMoving != null && link.target != null)
        {
            _trailMoving.SetTimeToMove(link.delay);
            _trailMoving.StartTrail(link.target.transform);
        }

        yield return new WaitForSeconds(link.delay);

        ExecuteLink(link, eventData);
    }

    private void ExecuteLink(CollapseLink link, CollapseEventData eventData)
    {
        if (link.target == null) return;

        // Защита от бесконечных цепочек
        if (_currentChainDepth >= maxChainDepth)
        {
            GameDebug.LogWarning(
                $"[CollapseLinkController] Chain depth limit ({maxChainDepth}) reached! " +
                $"Source: {name}, Target: {link.target.name}. Breaking chain.");
            return;
        }

        _currentChainDepth++;

        try
        {
            CollapseState? targetState = link.GetTargetState(eventData);
            link.target.CollapseByChain(targetState);
        }
        finally
        {
            _currentChainDepth--;
        }
    }

    #region Информация для дизайнеров

    /// <summary>
    /// Получить все описания связей для дебаг-оверлея.
    /// </summary>
    public List<string> GetLinkDescriptions()
    {
        var descriptions = new List<string>();
        foreach (var link in links)
        {
            descriptions.Add(link.GetDescription());
        }

        return descriptions;
    }

    /// <summary>
    /// Количество активных связей (с назначенными целями).
    /// </summary>
    public int ActiveLinkCount
    {
        get
        {
            int count = 0;
            foreach (var link in links)
            {
                if (link.target != null) count++;
            }

            return count;
        }
    }

    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        foreach (var link in links)
        {
            if (link.target == null) continue;

            // Цвет линии зависит от условия
            Color lineColor = link.triggerWhen switch
            {
                CollapseTriggerCondition.OnAnyCollapse => new Color(0.5f, 0.8f, 1f, 0.6f),
                CollapseTriggerCondition.OnCollapseToOld => new Color(0.8f, 0.6f, 0.2f, 0.6f),
                CollapseTriggerCondition.OnCollapseToNew => new Color(0.2f, 0.8f, 0.4f, 0.6f),
                CollapseTriggerCondition.OnPlayerCollapse => new Color(0.3f, 0.6f, 1f, 0.6f),
                CollapseTriggerCondition.OnChainCollapse => new Color(0.8f, 0.3f, 0.8f, 0.6f),
                CollapseTriggerCondition.OnTimerCollapse => new Color(1f, 0.3f, 0.3f, 0.6f),
                _ => Color.white
            };

            bool isSelected = UnityEditor.Selection.activeGameObject == gameObject ||
                              UnityEditor.Selection.activeGameObject == link.target.gameObject;

            if (isSelected)
            {
                UnityEditor.Handles.color = lineColor;
                UnityEditor.Handles.DrawLine(transform.position, link.target.transform.position, 3f);

                // Стрелка к цели
                Vector3 direction = (link.target.transform.position - transform.position).normalized;
                Vector3 arrowPos = link.target.transform.position - direction * 0.5f;
                UnityEditor.Handles.ConeHandleCap(0, arrowPos, Quaternion.LookRotation(direction), 0.3f,
                    EventType.Repaint);

                // Подпись
                Vector3 midPoint = (transform.position + link.target.transform.position) / 2f + Vector3.up * 0.3f;
                GUIStyle style = new GUIStyle
                {
                    normal = { textColor = lineColor },
                    fontSize = 10,
                    alignment = TextAnchor.MiddleCenter
                };
                UnityEditor.Handles.Label(midPoint, link.GetDescription(), style);
            }
            else
            {
                Gizmos.color = lineColor;
                Gizmos.DrawLine(transform.position, link.target.transform.position);
            }
        }
    }
#endif
}