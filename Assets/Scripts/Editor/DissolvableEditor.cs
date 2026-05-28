using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace CLEditor
{
    [CustomEditor(typeof(Dissolvable))]
    public class DissolvableEditor : Editor
    {
        private Dissolvable _targetScript;
        private SerializedProperty _renderersProp;
        private SerializedProperty _collidersProp;
        private SerializedProperty _timeToDissolveProp;
        private SerializedProperty _onTransitionStartedProp;
        private SerializedProperty _onTransitionEndedProp;
        private SerializedProperty _onDissolvedProp;
        private SerializedProperty _onUndissolvedProp;

        private ReorderableList _renderersList;
        private ReorderableList _collidersList;

        private void OnEnable()
        {
            _targetScript = (Dissolvable)target;

            _renderersProp = serializedObject.FindProperty("renderers");
            _collidersProp = serializedObject.FindProperty("colliders");
            _timeToDissolveProp = serializedObject.FindProperty("timeToDissolve");
            _onTransitionStartedProp = serializedObject.FindProperty("OnTransitionStarted");
            _onTransitionEndedProp = serializedObject.FindProperty("OnTransitionEnded");
            _onDissolvedProp = serializedObject.FindProperty("OnDissolved");
            _onUndissolvedProp = serializedObject.FindProperty("OnUndissolved");

            // Инициализация списка рендереров
            if (_renderersProp != null)
            {
                _renderersList = new ReorderableList(serializedObject, _renderersProp, true, true, true, true)
                {
                    drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Список рендереров"),
                    drawElementCallback = (rect, index, isActive, isFocused) =>
                    {
                        var element = _renderersProp.GetArrayElementAtIndex(index);
                        rect.y += 2;
                        EditorGUI.PropertyField(
                            new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                            element, GUIContent.none);
                    }
                };
            }

            // Инициализация списка коллайдеров
            if (_collidersProp != null)
            {
                _collidersList = new ReorderableList(serializedObject, _collidersProp, true, true, true, true)
                {
                    drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Список коллайдеров"),
                    drawElementCallback = (rect, index, isActive, isFocused) =>
                    {
                        var element = _collidersProp.GetArrayElementAtIndex(index);
                        rect.y += 2;
                        EditorGUI.PropertyField(
                            new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                            element, GUIContent.none);
                    }
                };
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Рендереры
            EditorGUILayout.LabelField("Рендереры", EditorStyles.boldLabel);
            if (_renderersList != null)
            {
                _renderersList.DoLayoutList();
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
                    if (_targetScript != null)
                    {
                        Undo.RecordObject(_targetScript, "Set Renderer This");
                        _targetScript.SetRendererThis();
                        EditorUtility.SetDirty(_targetScript);
                        serializedObject.Update();
                    }
                }

                if (GUILayout.Button("Взять из дочерних", GUILayout.Height(25)))
                {
                    if (_targetScript != null)
                    {
                        Undo.RecordObject(_targetScript, "Set Renderers In Children");
                        _targetScript.SetRenderersInChildren();
                        EditorUtility.SetDirty(_targetScript);
                        serializedObject.Update();
                    }
                }
            }

            // Проверка на пустой список рендереров
            if (_targetScript != null && _targetScript.renderers.Count == 0)
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
            if (_collidersList != null)
            {
                _collidersList.DoLayoutList();
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
                    if (_targetScript != null)
                    {
                        Undo.RecordObject(_targetScript, "Set Collider This");
                        _targetScript.SetColliderThis();
                        EditorUtility.SetDirty(_targetScript);
                        serializedObject.Update();
                    }
                }

                if (GUILayout.Button("Взять из дочерних", GUILayout.Height(25)))
                {
                    if (_targetScript != null)
                    {
                        Undo.RecordObject(_targetScript, "Set Colliders In Children");
                        _targetScript.SetCollidersInChildren();
                        EditorUtility.SetDirty(_targetScript);
                        serializedObject.Update();
                    }
                }
            }

            // Проверка на пустой список коллайдеров
            if (_targetScript != null && _targetScript.colliders.Count == 0)
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
                "Убедитесь, что материалы используют шейдер с параметром _Dissolve (float).\n" +
                "Рекомендуется: Custom/TemporalInstability",
                MessageType.Info
            );

            // Настройки диссольва
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Настройки диссолва", EditorStyles.boldLabel);
            if (_timeToDissolveProp != null)
            {
                EditorGUILayout.PropertyField(_timeToDissolveProp, new GUIContent("Время диссолва"));
            }

            // События
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("События", EditorStyles.boldLabel);
            if (_onTransitionStartedProp != null)
                EditorGUILayout.PropertyField(_onTransitionStartedProp, new GUIContent("Начало перехода"));
            if (_onTransitionEndedProp != null)
                EditorGUILayout.PropertyField(_onTransitionEndedProp, new GUIContent("Конец перехода"));
            if (_onDissolvedProp != null)
                EditorGUILayout.PropertyField(_onDissolvedProp, new GUIContent("Объект исчез"));
            if (_onUndissolvedProp != null)
                EditorGUILayout.PropertyField(_onUndissolvedProp, new GUIContent("Объект появился"));

            // Управление диссольвом
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Управление диссолвом", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Dissolve", GUILayout.Height(25)) && _targetScript != null)
                {
                    _targetScript.Dissolve();
                }

                if (GUILayout.Button("Undissolve", GUILayout.Height(25)) && _targetScript != null)
                {
                    _targetScript.Undissolve();
                }
            }

            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
        }

        private void OnDisable()
        {
            // Безопасная очистка — проверяем что объекты не null перед обнулением
            if (_renderersList != null)
            {
                _renderersList = null;
            }

            if (_collidersList != null)
            {
                _collidersList = null;
            }

            // Обнуляем SerializedProperty только если они были инициализированы
            _renderersProp = null;
            _collidersProp = null;
            _timeToDissolveProp = null;
            _onTransitionStartedProp = null;
            _onTransitionEndedProp = null;
            _onDissolvedProp = null;
            _onUndissolvedProp = null;

            // Обнуляем target только если он не был уничтожен Unity
            _targetScript = null;
        }
    }
}