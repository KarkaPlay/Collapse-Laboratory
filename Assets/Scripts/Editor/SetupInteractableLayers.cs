#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CLEditor
{
    public class SetupInteractableLayers
    {
        [MenuItem("Tools/Collapse Lab/Setup Interactable Layers")]
        public static void SetupLayers()
        {
            int interactableLayer = LayerMask.NameToLayer("Interactable");

            if (interactableLayer < 0)
            {
                EditorUtility.DisplayDialog("Ошибка",
                    "Layer 'Interactable' не найден!\n\n" +
                    "Создайте его в Edit → Project Settings → Tags and Layers",
                    "OK");
                return;
            }

            int count = 0;

            // Все Collapsible
            var collapsibles = Object.FindObjectsByType<Collapsible>(FindObjectsSortMode.None);
            foreach (var c in collapsibles)
            {
                SetLayerRecursively(c.gameObject, interactableLayer);
                count++;
            }

            // Все Interactable
            var interactables = Object.FindObjectsByType<Interactable>(FindObjectsSortMode.None);
            foreach (var i in interactables)
            {
                SetLayerRecursively(i.gameObject, interactableLayer);
                count++;
            }

            Debug.Log($"✓ Установлен Layer 'Interactable' для {count} объектов и их детей");
        }

        private static void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }
}
#endif