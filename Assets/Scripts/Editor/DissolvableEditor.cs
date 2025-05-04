using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

[CustomEditor(typeof(Dissolvable))]
public class DissolvableEditor : Editor
{
    private Dissolvable targetScript;
    private SerializedProperty renderersProp;
    private SerializedProperty collidersProp;
    private SerializedProperty timeToDissolveProp;
    private SerializedProperty onTransitionStartedProp;
    private SerializedProperty onTransitionEndedProp;
    private SerializedProperty onDissolvedProp;
    private SerializedProperty onUndissolvedProp;

    private ReorderableList renderersList;
    private ReorderableList collidersList;

    void OnEnable()
    {
        targetScript = (Dissolvable)target;
        if (serializedObject == null) return;

        renderersProp = serializedObject.FindProperty("renderers");
        collidersProp = serializedObject.FindProperty("colliders");
        timeToDissolveProp = serializedObject.FindProperty("timeToDissolve");
        onTransitionStartedProp = serializedObject.FindProperty("OnTransitionStarted");
        onTransitionEndedProp = serializedObject.FindProperty("OnTransitionEnded");
        onDissolvedProp = serializedObject.FindProperty("OnDissolved");
        onUndissolvedProp = serializedObject.FindProperty("OnUndissolved");

        // Инициализация списка рендереров
        if (renderersProp != null)
        {
            renderersList = new ReorderableList(serializedObject, renderersProp, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Список рендереров"),
                drawElementCallback = (rect, index, active, focused) =>
                {
                    var element = renderersProp.GetArrayElementAtIndex(index);
                    rect.y += 2;
                    EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                        element, GUIContent.none);
                }
            };
        }

        // Инициализация списка коллайдеров
        if (collidersProp != null)
        {
            collidersList = new ReorderableList(serializedObject, collidersProp, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Список коллайдеров"),
                drawElementCallback = (rect, index, active, focused) =>
                {
                    var element = collidersProp.GetArrayElementAtIndex(index);
                    rect.y += 2;
                    EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                        element, GUIContent.none);
                }
            };
        }

        if (renderersProp == null || collidersProp == null)
        {
            Debug.LogError("Не удалось найти свойства 'renderers' или 'colliders' в Dissolvable. Проверьте сериализацию.");
        }
    }

    public override void OnInspectorGUI()
    {
        if (serializedObject == null)
        {
            EditorGUILayout.HelpBox("SerializedObject не инициализирован. Обновите инспектор.", MessageType.Error);
            return;
        }

        serializedObject.Update();

        // Рендереры
        EditorGUILayout.LabelField("Рендереры", EditorStyles.boldLabel);
        if (renderersList != null)
        {
            renderersList.DoLayoutList();
        }
        else
        {
            EditorGUILayout.HelpBox("Не удалось инициализировать список рендереров.", MessageType.Error);
        }

        // Кнопки для рендереров
        EditorGUILayout.Space();
        using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            if (GUILayout.Button("Взять из этого объекта", GUILayout.Height(25)))
            {
                if (targetScript != null)
                {
                    targetScript.SetRendererThis();
                    EditorUtility.SetDirty(targetScript);
                    Debug.Log($"Добавлен из этого объекта");
                }
            }

            if (GUILayout.Button("Взять из дочерних", GUILayout.Height(25)))
            {
                if (targetScript != null)
                {
                    targetScript.SetRenderersInChildren();
                    EditorUtility.SetDirty(targetScript);
                    Debug.Log($"Добавлены рендереры из дочерних объектов {targetScript.gameObject.name}");
                }
            }
        }

        // Проверка на пустой список рендереров
        if (targetScript != null && targetScript.renderers.Count == 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Список рендереров пуст. Добавьте рендереры вручную или используйте кнопки выше.",
                MessageType.Warning
            );
        }

        // Коллайдеры
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Коллайдеры", EditorStyles.boldLabel);
        if (collidersList != null)
        {
            collidersList.DoLayoutList();
        }
        else
        {
            EditorGUILayout.HelpBox("Не удалось инициализировать список коллайдеров.", MessageType.Error);
        }

        // Кнопки для коллайдеров
        EditorGUILayout.Space();
        using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
        {
            if (GUILayout.Button("Взять из этого объекта", GUILayout.Height(25)))
            {
                if (targetScript != null)
                {
                    targetScript.SetColliderThis();
                    EditorUtility.SetDirty(targetScript);
                    Debug.Log($"Добавлен коллайдер из {targetScript.gameObject.name}");
                }
            }

            if (GUILayout.Button("Взять из дочерних", GUILayout.Height(25)))
            {
                if (targetScript != null)
                {
                    targetScript.SetCollidersInChildren();
                    EditorUtility.SetDirty(targetScript);
                    Debug.Log($"Добавлены коллайдеры из дочерних объектов {targetScript.gameObject.name}");
                }
            }
        }

        // Проверка на пустой список коллайдеров
        if (targetScript != null && targetScript.colliders.Count == 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Список коллайдеров пуст. Добавьте коллайдеры вручную или используйте кнопки выше.",
                MessageType.Warning
            );
        }

        // Предупреждение о требованиях к шейдеру
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Убедитесь, что материалы используют шейдер с параметром _Dissolve (float).",
            MessageType.Info
        );

        // Настройки диссольва
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Настройки диссолва", EditorStyles.boldLabel);
        if (timeToDissolveProp != null)
        {
            EditorGUILayout.PropertyField(timeToDissolveProp, new GUIContent("Время диссолва"));
        }

        // События
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("События", EditorStyles.boldLabel);
        if (onTransitionStartedProp != null)
            EditorGUILayout.PropertyField(onTransitionStartedProp, new GUIContent("Начало перехода"));
        if (onTransitionEndedProp != null)
            EditorGUILayout.PropertyField(onTransitionEndedProp, new GUIContent("Конец перехода"));
        if (onDissolvedProp != null)
            EditorGUILayout.PropertyField(onDissolvedProp, new GUIContent("Объект исчез"));
        if (onUndissolvedProp != null)
            EditorGUILayout.PropertyField(onUndissolvedProp, new GUIContent("Объект появился"));

        // Управление диссольвом
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Управление диссолвом", EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(!Application.isPlaying);
        using (new GUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Dissolve", GUILayout.Height(25)) && targetScript != null)
            {
                targetScript.Dissolve();
            }
            if (GUILayout.Button("Undissolve", GUILayout.Height(25)) && targetScript != null)
            {
                targetScript.Undissolve();
            }
        }
        EditorGUI.EndDisabledGroup();

        serializedObject.ApplyModifiedProperties();
    }
}