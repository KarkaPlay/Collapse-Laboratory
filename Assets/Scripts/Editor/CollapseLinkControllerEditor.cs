using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(CollapseLinkController))]
public class CollapseLinkControllerEditor : Editor
{
    private ReorderableList _list;

    private SerializedProperty _linksProp;
    private SerializedProperty _maxChainDepthProp;

    private const float LINE_HEIGHT = 18f;
    private const float VERTICAL_SPACING = 4f;

    private void OnEnable()
    {
        _linksProp = serializedObject.FindProperty("links");
        _maxChainDepthProp = serializedObject.FindProperty("maxChainDepth");

        _list = new ReorderableList(serializedObject, _linksProp, true, true, true, true);

        _list.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect, "Связи (Collapse Links)");
        };

        _list.elementHeightCallback = index =>
        {
            // Считаем высоту вручную (чтобы ничего не обрезалось)
            int lines = 6; // target, trigger, action, delay, showTrail, note
            float noteHeight = EditorGUI.GetPropertyHeight(
                _linksProp.GetArrayElementAtIndex(index).FindPropertyRelative("designerNote"),
                true
            );

            return (lines * (LINE_HEIGHT + VERTICAL_SPACING)) + noteHeight + 8;
        };

        _list.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            var element = _linksProp.GetArrayElementAtIndex(index);

            var targetProp = element.FindPropertyRelative("target");
            var triggerProp = element.FindPropertyRelative("triggerWhen");
            var actionProp = element.FindPropertyRelative("action");
            var delayProp = element.FindPropertyRelative("delay");
            var showTrailProp = element.FindPropertyRelative("showTrail");
            var noteProp = element.FindPropertyRelative("designerNote");

            rect.y += 4;

            float y = rect.y;
            float width = rect.width;

            // 1. Target (на всю ширину)
            EditorGUI.PropertyField(
                new Rect(rect.x, y, width, LINE_HEIGHT),
                targetProp
            );
            y += LINE_HEIGHT + VERTICAL_SPACING;

            // 2. Trigger + Action (в одну строку)
            float half = (width - 4) / 2f;

            EditorGUI.PropertyField(
                new Rect(rect.x, y, half, LINE_HEIGHT),
                triggerProp,
                GUIContent.none
            );

            EditorGUI.PropertyField(
                new Rect(rect.x + half + 4, y, half, LINE_HEIGHT),
                actionProp,
                GUIContent.none
            );

            y += LINE_HEIGHT + VERTICAL_SPACING;

            // 3. Delay + ShowTrail (в одну строку)
            EditorGUI.PropertyField(
                new Rect(rect.x, y, half, LINE_HEIGHT),
                delayProp
            );

            EditorGUI.PropertyField(
                new Rect(rect.x + half + 4, y, half, LINE_HEIGHT),
                showTrailProp,
                new GUIContent("Show Trail")
            );

            y += LINE_HEIGHT + VERTICAL_SPACING;

            // 4. Designer Note (многострочное)
            float noteHeight = EditorGUI.GetPropertyHeight(noteProp, true);
            EditorGUI.PropertyField(
                new Rect(rect.x, y, width, noteHeight),
                noteProp
            );
        };

        _list.onAddCallback = list =>
        {
            _linksProp.arraySize++;
            serializedObject.ApplyModifiedProperties();

            var element = _linksProp.GetArrayElementAtIndex(_linksProp.arraySize - 1);

            element.FindPropertyRelative("delay").floatValue = 0.3f;
            element.FindPropertyRelative("showTrail").boolValue = true;
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space();

        _list.DoLayoutList();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Настройки", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_maxChainDepthProp);

        serializedObject.ApplyModifiedProperties();
    }
}