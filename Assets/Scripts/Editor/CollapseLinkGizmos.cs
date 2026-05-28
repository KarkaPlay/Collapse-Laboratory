#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CLEditor
{
    /// <summary>
    /// Gizmos для CollapseLinkController — показывает связи между объектами.
    /// </summary>
    [InitializeOnLoad]
    public static class CollapseLinkGizmos
    {
        static CollapseLinkGizmos()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            var allLinks = Object.FindObjectsByType<CollapseLinkController>(FindObjectsSortMode.None);

            foreach (var linkCtrl in allLinks)
            {
                if (linkCtrl == null || linkCtrl.links == null) continue;

                DrawLinkGizmos(linkCtrl);
            }
        }

        private static void DrawLinkGizmos(CollapseLinkController linkCtrl)
        {
            var sourcePos = linkCtrl.transform.position;
            bool isSelected = Selection.activeGameObject == linkCtrl.gameObject;

            foreach (var link in linkCtrl.links)
            {
                if (link == null || link.target == null) continue;

                DrawLink(sourcePos, link, isSelected);
            }
        }

        private static void DrawLink(Vector3 sourcePos, CollapseLink link, bool isSelected)
        {
            var targetPos = link.target.transform.position;

            // Цвет линии по условию
            Color lineColor = link.triggerWhen switch
            {
                CollapseTriggerCondition.OnAnyCollapse => new Color(0.5f, 0.8f, 1f, 0.8f),
                CollapseTriggerCondition.OnCollapseToOld => new Color(0.9f, 0.7f, 0.3f, 0.8f),
                CollapseTriggerCondition.OnCollapseToNew => new Color(0.3f, 0.9f, 0.5f, 0.8f),
                CollapseTriggerCondition.OnPlayerCollapse => new Color(0.4f, 0.7f, 1f, 0.8f),
                CollapseTriggerCondition.OnChainCollapse => new Color(0.9f, 0.4f, 0.9f, 0.8f),
                CollapseTriggerCondition.OnTimerCollapse => new Color(1f, 0.4f, 0.4f, 0.8f),
                _ => new Color(0.7f, 0.7f, 0.7f, 0.6f)
            };

            if (!isSelected)
            {
                lineColor.a *= 0.5f;
            }

            Handles.color = lineColor;

            // Рисуем линию
            if (isSelected)
            {
                Handles.DrawLine(sourcePos, targetPos, 4f);

                // Стрелка к цели
                DrawArrow(sourcePos, targetPos, lineColor);

                // Метка связи
                Vector3 midPoint = (sourcePos + targetPos) / 2f + Vector3.up * 0.3f;
                DrawLinkLabel(midPoint, link, lineColor);
            }
            else
            {
                Handles.DrawDottedLine(sourcePos, targetPos, 3f);
            }

            // Сфера на цели
            if (isSelected)
            {
                Handles.SphereHandleCap(0, targetPos, Quaternion.identity, 0.25f, EventType.Repaint);
            }
        }

        private static void DrawArrow(Vector3 from, Vector3 to, Color color)
        {
            Vector3 direction = (to - from).normalized;
            Vector3 arrowPos = to - direction * 0.5f;

            Handles.color = color;
            Handles.ConeHandleCap(0, arrowPos, Quaternion.LookRotation(direction), 0.4f, EventType.Repaint);
        }

        private static void DrawLinkLabel(Vector3 position, CollapseLink link, Color color)
        {
            string conditionShort = link.triggerWhen switch
            {
                CollapseTriggerCondition.OnAnyCollapse => "Any",
                CollapseTriggerCondition.OnCollapseToOld => "→Old",
                CollapseTriggerCondition.OnCollapseToNew => "→New",
                CollapseTriggerCondition.OnPlayerCollapse => "Player",
                CollapseTriggerCondition.OnChainCollapse => "Chain",
                CollapseTriggerCondition.OnTimerCollapse => "Timer",
                _ => "?"
            };

            string actionShort = link.action switch
            {
                CollapseLinkAction.Toggle => "Toggle",
                CollapseLinkAction.SetToOld => "→Old",
                CollapseLinkAction.SetToNew => "→New",
                CollapseLinkAction.MatchSource => "=Src",
                CollapseLinkAction.InvertSource => "≠Src",
                _ => "?"
            };

            string label = $"{conditionShort} | {actionShort}";
            if (link.delay > 0)
            {
                label += $"\n⏱{link.delay:F1}s";
            }

            if (!string.IsNullOrEmpty(link.designerNote))
            {
                label += $"\n📝 {link.designerNote}";
            }

            GUIStyle style = new GUIStyle
            {
                normal =
                {
                    textColor = color,
                    background = MakeBackgroundTexture()
                },
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(4, 4, 2, 2)
            };

            Handles.Label(position, label, style);
        }

        private static Texture2D _bgTexture;

        private static Texture2D MakeBackgroundTexture()
        {
            if (_bgTexture == null)
            {
                _bgTexture = new Texture2D(1, 1);
                _bgTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.7f));
                _bgTexture.Apply();
            }

            return _bgTexture;
        }
    }
}
#endif