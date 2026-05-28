#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CLEditor
{
    /// <summary>
    /// Gizmos для CollapsibleGroupController — показывает связи между объектами группы.
    /// </summary>
    [InitializeOnLoad]
    public static class CollapsibleGroupGizmos
    {
        static CollapsibleGroupGizmos()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            var allGroups = Object.FindObjectsByType<CollapsibleGroupController>(FindObjectsSortMode.None);

            foreach (var group in allGroups)
            {
                if (group == null) continue;

                DrawGroupGizmo(group);
            }
        }

        private static void DrawGroupGizmo(CollapsibleGroupController group)
        {
            if (group.Collapsibles == null || group.Collapsibles.Count < 2) return;

            bool isSelected = Selection.activeGameObject == group.gameObject;

            // Цвет по паттерну
            Color lineColor = group.pattern switch
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

            Handles.color = lineColor;

            // Линии между объектами
            for (int i = 0; i < group.Collapsibles.Count; i++)
            {
                if (group.Collapsibles[i] == null) continue;
                var startPos = group.Collapsibles[i].transform.position;

                for (int j = i + 1; j < group.Collapsibles.Count; j++)
                {
                    if (group.Collapsibles[j] == null) continue;
                    var endPos = group.Collapsibles[j].transform.position;

                    if (isSelected)
                    {
                        Handles.DrawLine(startPos, endPos, 3f);
                    }
                    else
                    {
                        Handles.DrawDottedLine(startPos, endPos, 2f);
                    }
                }

                // Сферы на объектах
                if (isSelected)
                {
                    Handles.SphereHandleCap(0, startPos, Quaternion.identity, 0.2f, EventType.Repaint);
                }
            }

            // Подпись при выделении
            if (isSelected)
            {
                string info = $"⚡ {group.pattern}\nInterval: {group.switchStateInterval:F1}s";
                if (group.pattern == InstabilityPattern.Accelerating)
                    info += $"\nMin: {group.minInterval:F1}s";
                if (group.pattern == InstabilityPattern.Clustered)
                    info += $"\nCluster: {group.clusterSize}";

                GUIStyle style = new GUIStyle
                {
                    normal = { textColor = lineColor },
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };

                Handles.Label(group.transform.position + Vector3.up * 1f, info, style);
            }
        }
    }
}
#endif