#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CLEditor
{
    /// <summary>
    /// Gizmos для Interactable объектов — показывает связи OnInteractEvent.
    /// </summary>
    [InitializeOnLoad]
    public static class InteractableGizmos
    {
        static InteractableGizmos()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private static void OnSceneGUI(SceneView sceneView)
        {
            var allInteractables = Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None);

            foreach (var interactable in allInteractables)
            {
                if (interactable == null) continue;

                DrawInteractableGizmo(interactable);
            }
        }

        private static void DrawInteractableGizmo(Interactable interactable)
        {
            if (interactable.OnInteractEvent == null) return;

            var position = interactable.transform.position;
            bool isSelected = Selection.activeGameObject == interactable.gameObject;

            // Рисуем линии к целям OnInteractEvent
            for (int i = 0; i < interactable.OnInteractEvent.GetPersistentEventCount(); i++)
            {
                var target = interactable.OnInteractEvent.GetPersistentTarget(i);
                if (target == null) continue;

                Transform targetTransform = null;

                if (target is Component component)
                {
                    targetTransform = component.transform;
                }
                else if (target is GameObject go)
                {
                    targetTransform = go.transform;
                }

                if (targetTransform == null) continue;

                if (isSelected)
                {
                    // Толстая зелёная линия для выделенного
                    Handles.color = new Color(0f, 1f, 0f, 0.8f);
                    Handles.DrawLine(position, targetTransform.position, 3f);

                    // Сфера на цели
                    Handles.SphereHandleCap(0, targetTransform.position, Quaternion.identity, 0.2f,
                        EventType.Repaint);
                }
                else
                {
                    // Тонкая линия для не выделенного
                    Handles.color = new Color(0f, 1f, 0f, 0.5f);
                    Handles.DrawDottedLine(position, targetTransform.position, 2f);
                }
            }
        }
    }
}
#endif