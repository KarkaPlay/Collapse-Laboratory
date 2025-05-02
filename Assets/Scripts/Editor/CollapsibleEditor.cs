using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Collapsible))]
public class CollapsibleEditor : Editor
{
    public override void OnInspectorGUI()
    {
        Collapsible collapsible = (Collapsible)target;

        // Сериализуемые свойства
        SerializedProperty stateOldProp = serializedObject.FindProperty("stateOld");
        SerializedProperty stateNewProp = serializedObject.FindProperty("stateNew");
        SerializedProperty initialStateProp = serializedObject.FindProperty("initialState");
        SerializedProperty isDynamicProp = serializedObject.FindProperty("isDynamic");
        SerializedProperty canPlayerCollapseProp = serializedObject.FindProperty("canPlayerCollapse");

        serializedObject.Update();

        // Заголовок секции "Объекты состояний"
        EditorGUILayout.LabelField("Объекты состояний", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(stateOldProp);
        EditorGUILayout.PropertyField(stateNewProp);

        // Кнопка для установки состояний из дочерних объектов
        EditorGUILayout.Space();
        if (GUILayout.Button("Set COStates from Children", GUILayout.Height(25)))
        {
            collapsible.SetCOStatesFromChildren();
            EditorUtility.SetDirty(collapsible);
            Debug.Log($"COStates updated for {collapsible.gameObject.name}");
        }

        // Проверка на null для состояний
        if (collapsible.stateOld == null || collapsible.stateNew == null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Один или оба объекта состояний не назначены. Используйте кнопку выше или назначьте вручную.", MessageType.Warning);
        }

        // Секция начального и текущего состояния
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Состояние", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(initialStateProp);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Текущее состояние");
        EditorGUILayout.LabelField(collapsible.CurrentState.ToString());
        EditorGUILayout.EndHorizontal();

        // Кнопки управления состоянием
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Управление состоянием", EditorStyles.boldLabel);
        using (new GUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Collapse (Toggle)", GUILayout.Height(22)))
            {
                collapsible.Collapse();
            }

            if (GUILayout.Button("Reset", GUILayout.Height(22)))
            {
                collapsible.Reset();
            }
        }

        // Секция параметров
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Параметры", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(isDynamicProp);
        EditorGUILayout.PropertyField(canPlayerCollapseProp);

        // Сохранение изменений
        serializedObject.ApplyModifiedProperties();
    }
}