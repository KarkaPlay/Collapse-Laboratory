using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(COState))]
public class COStateEditor : Editor
{
    public override void OnInspectorGUI()
    {
        COState coState = (COState)target;

        // Сериализуемые свойства
        SerializedProperty parentCollapsibleProp = serializedObject.FindProperty("parentCollapsible");
        SerializedProperty outlineProp = serializedObject.FindProperty("outline");
        SerializedProperty dissolvableProp = serializedObject.FindProperty("dissolvable");

        serializedObject.Update();

        // Заголовок компонента
        EditorGUILayout.LabelField("Основные настройки", EditorStyles.boldLabel);

        // Поле для родительского Collapsible
        EditorGUILayout.PropertyField(parentCollapsibleProp);

        // Поле для Outline
        EditorGUILayout.PropertyField(outlineProp);

        // Поле для Dissolve
        EditorGUILayout.PropertyField(dissolvableProp);

        // Кнопка автоматического поиска родителя, Outline и Dissolve
        EditorGUILayout.Space();
        if (GUILayout.Button("Автонастройка: Родитель + Outline + Dissolve", GUILayout.Height(25)))
        {
            coState.SetParentOutlineAndDissolve();
            EditorUtility.SetDirty(coState);
            Debug.Log($"Родитель, Outline и Dissolve обновлены для {coState.gameObject.name}");
        }

        // Проверка наличия ссылок
        EditorGUILayout.Space();
        if (coState.parentCollapsible == null)
        {
            EditorGUILayout.HelpBox("Родитель (Collapsible) не назначен. Используйте кнопку автонастройки или назначьте вручную.", MessageType.Error);
        }
        if (coState.Outline == null)
        {
            EditorGUILayout.HelpBox("Outline не назначен. Используйте кнопку автонастройки или добавьте компонент Outline вручную.", MessageType.Error);
        }
        if (coState.Dissolvable == null)
        {
            EditorGUILayout.HelpBox("Dissolvable не назначен. Используйте кнопку автонастройки или добавьте компонент Outline вручную.", MessageType.Error);
        }

        // Отображение текущего состояния
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Состояние", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel("Это состояние");
        EditorGUILayout.LabelField(GetStateFromName(coState.gameObject.name));
        EditorGUILayout.EndHorizontal();

        // Кнопки для тестирования методов
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Тестирование методов", EditorStyles.boldLabel);
        using (new GUILayout.HorizontalScope())
        {
            if (GUILayout.Button("OnCollapse", GUILayout.Height(22)))
            {
                coState.OnCollapse();
            }

            if (GUILayout.Button("Highlight", GUILayout.Height(22)))
            {
                coState.OnCollapseHighlight();
            }

            if (GUILayout.Button("Unhighlight", GUILayout.Height(22)))
            {
                coState.OnCollapseUnhighlight();
            }
        }

        // Сохранение изменений
        serializedObject.ApplyModifiedProperties();
    }

    // Автоматическое определение состояния по имени объекта
    private string GetStateFromName(string name)
    {
        if (name.EndsWith("_OLD")) return "Old";
        if (name.EndsWith("_NEW")) return "New";
        return "Не определено";
    }
}