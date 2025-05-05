using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(CollapsibleGroupController))]
public class CollapsileGroupControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        CollapsibleGroupController controller = (CollapsibleGroupController)target;

        SerializedProperty collapsiblesProp = serializedObject.FindProperty("collapsibles");
        SerializedProperty intervalProp = serializedObject.FindProperty("switchStateInterval");

        serializedObject.Update();

        // Заголовки полей
        EditorGUILayout.LabelField("Схлопывающиеся элементы", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(collapsiblesProp, true); // true для раскрытия списка
        
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(intervalProp);

        EditorGUILayout.Space();
        
        if (GUILayout.Button("Set Collapsibles from Children", GUILayout.Height(25)))
        {
            controller.SetCollapsiblesFromChildren();
            EditorUtility.SetDirty(controller);
            Debug.Log($"Collapsibles updated from children for {controller.gameObject.name}");
        }

        EditorGUILayout.Space();
        
        EditorGUI.BeginDisabledGroup(!Application.isPlaying);
        EditorGUILayout.LabelField("Coroutine Controls", EditorStyles.boldLabel);
        using (new GUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Start Switching", GUILayout.Height(22)))
            {
                controller.StartDynamicStateSwitching();
            }
            if (GUILayout.Button("Stop Switching", GUILayout.Height(22)))
            {
                controller.StopDynamicStateSwitching();
            }
        }
        EditorGUI.EndDisabledGroup();

        // Предупреждение о пустом списке
        if (controller.Collapsibles.Count == 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "В списке Collapsibles нет элементов.\n" +
                "Добавьте их вручную или нажмите кнопку 'Set Collapsibles from Children'",
                MessageType.Warning
            );
        }

        serializedObject.ApplyModifiedProperties();
    }
}