#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CLEditor
{
    /// <summary>
    /// Gizmos для Collapsible объектов.
    /// </summary>
    [InitializeOnLoad]
    public static class CollapsibleGizmos
    {
        static CollapsibleGizmos()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            var allCollapsibles = Object.FindObjectsByType<Collapsible>(FindObjectsSortMode.None);

            foreach (var c in allCollapsibles)
            {
                if (c == null) continue;

                DrawCollapsibleGizmo(c);
            }
        }

        private static void DrawCollapsibleGizmo(Collapsible c)
        {
            // Цвет по стабильности
            Color gizmoColor = c.GetStabilityColor();
            Handles.color = gizmoColor;

            Vector3 position = c.transform.position;

            // Маленький куб
            Handles.DrawWireCube(position, Vector3.one * 0.3f);

            // Для нестабильных — пульсирующий эффект
            if (c.stabilityLevel == StabilityLevel.Unstable)
            {
                float pulse = Mathf.Sin((float)EditorApplication.timeSinceStartup * 3f) * 0.5f + 0.5f;
                Color c2 = gizmoColor;
                c2.a = pulse * 0.5f;
                Handles.color = c2;
                Handles.CubeHandleCap(0, position, Quaternion.identity, 0.35f, EventType.Repaint);
            }

            // Таймер для динамических объектов
            var groupController = c.GetComponentInParent<CollapsibleGroupController>();
            if (c.IsDynamic && groupController != null)
            {
                float timeRemaining =
                    Mathf.Max(0, groupController.switchStateInterval - groupController.TimeSinceLastSwitch);
                string timeText = $"{c.stabilityLevel} | Next: {timeRemaining:F1}s";

                Vector3 labelPosition = position + Vector3.up * 0.5f;

                GUIStyle style = new GUIStyle
                {
                    normal = { textColor = gizmoColor },
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };

                Handles.Label(labelPosition, timeText, style);
            }

            // Детальная информация при выделении
            if (Selection.activeGameObject == c.gameObject)
            {
                DrawSelectedInfo(c, position);
            }
        }

        private static void DrawSelectedInfo(Collapsible c, Vector3 position)
        {
            Vector3 labelPosition = position + Vector3.up * 1f;

            GUIStyle style = new GUIStyle
            {
                normal =
                {
                    textColor = Color.white,
                    background = MakeBackgroundTexture()
                },
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(5, 5, 3, 3)
            };

            string info = $"[{c.stabilityLevel}] {c.CurrentState}\n" +
                          $"Player: {(c.CanPlayerCollapse ? "✓" : "✗")} | " +
                          $"Chain: {(c.CanBeLinkedTarget ? "✓" : "✗")} | " +
                          $"Timer: {(c.IsDynamic ? "✓" : "✗")}";

            Handles.Label(labelPosition, info, style);
        }

        private static Texture2D _bgTexture;

        private static Texture2D MakeBackgroundTexture()
        {
            if (_bgTexture == null)
            {
                _bgTexture = new Texture2D(1, 1);
                _bgTexture.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.1f, 0.8f));
                _bgTexture.Apply();
            }

            return _bgTexture;
        }
    }
}
#endif