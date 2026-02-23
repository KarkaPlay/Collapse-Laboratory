using Objects;
using UnityEditor;
using UnityEngine;

namespace CLEditor
{
    [CustomEditor(typeof(AnimatedCollapsible))]
    public class AnimatedCollapsibleEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(15);
            GUILayout.Box("", GUILayout.Height(1), GUILayout.ExpandWidth(true));
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("Автонастройка дочерних объектов", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Эта функция добавит компоненты AnimatedCollapsibleChild и коллайдеры ко всем дочерним объектам, у которых их нет",
                MessageType.Info);

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Настроить дочерние объекты", GUILayout.Width(200), GUILayout.Height(30)))
            {
                AnimatedCollapsible targetComponent = (AnimatedCollapsible)target;
                targetComponent.SetChildren();
                EditorUtility.SetDirty(target);
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "После настройки проверьте дочерние объекты:\n" +
                "1. У каждого должен быть компонент AnimatedCollapsibleChild\n" +
                "2. У каждого должен быть коллайдер",
                MessageType.Warning);
        }
    }
}