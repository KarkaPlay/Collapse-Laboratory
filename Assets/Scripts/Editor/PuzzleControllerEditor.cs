using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace CLEditor
{
    [CustomEditor(typeof(PuzzleController))]
    public class PuzzleControllerEditor : Editor
    {
        private ReorderableList _conditionsList;
        private SerializedProperty _conditionsProp;
        private bool _showDetailedStatus = true;

        private void OnEnable()
        {
            _conditionsProp = serializedObject.FindProperty("conditions");

            _conditionsList = new ReorderableList(serializedObject, _conditionsProp, true, true, true, true)
            {
                drawHeaderCallback = rect =>
                {
                    EditorGUI.LabelField(rect, $"Условия решения — {_conditionsProp.arraySize} шт.");
                },
                elementHeightCallback = index => EditorGUIUtility.singleLineHeight * 3 + 12,
                drawElementCallback = DrawConditionElement
            };
        }

        private void DrawConditionElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = _conditionsProp.GetArrayElementAtIndex(index);
            float lineHeight = EditorGUIUtility.singleLineHeight + 2;
            float y = rect.y + 4;

            // Проверяем выполнение условия
            PuzzleController controller = (PuzzleController)target;
            bool isSatisfied = false;
            if (index < controller.conditions.Count)
            {
                isSatisfied = controller.conditions[index].IsSatisfied;
            }

            // Иконка статуса
            string statusIcon = isSatisfied ? "✅" : "❌";

            // Target + Required State
            float halfWidth = (rect.width - 30) / 2;

            EditorGUI.LabelField(
                new Rect(rect.x, y, 20, EditorGUIUtility.singleLineHeight),
                statusIcon);

            EditorGUI.PropertyField(
                new Rect(rect.x + 25, y, halfWidth, EditorGUIUtility.singleLineHeight),
                element.FindPropertyRelative("target"),
                new GUIContent("Объект"));

            EditorGUI.PropertyField(
                new Rect(rect.x + halfWidth + 35, y, halfWidth - 5, EditorGUIUtility.singleLineHeight),
                element.FindPropertyRelative("requiredState"),
                new GUIContent("Нужно"));

            y += lineHeight;

            // Note
            EditorGUI.PropertyField(
                new Rect(rect.x + 25, y, rect.width - 25, EditorGUIUtility.singleLineHeight),
                element.FindPropertyRelative("note"),
                new GUIContent("📝 Заметка"));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            PuzzleController controller = (PuzzleController)target;

            // === Заголовок ===
            Color headerColor = controller.IsSolved
                ? new Color(0.3f, 0.9f, 0.4f)
                : new Color(1f, 0.85f, 0.3f);

            GUI.backgroundColor = headerColor;
            GUIStyle headerStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 8, 8)
            };

            string solvedText = controller.IsSolved ? "✅ РЕШЕНА" : "🧩 НЕ РЕШЕНА";
            EditorGUILayout.LabelField(
                $"{solvedText} — {controller.puzzleName}",
                headerStyle);
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(5);

            // === Информация ===
            EditorGUILayout.PropertyField(serializedObject.FindProperty("puzzleName"),
                new GUIContent("Название"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("puzzleDescription"),
                new GUIContent("Описание"));

            EditorGUILayout.Space(5);

            // === Прогресс ===
            EditorGUI.ProgressBar(
                EditorGUILayout.GetControlRect(false, 20),
                controller.Progress,
                $"Прогресс: {(controller.Progress * 100):F0}%");

            EditorGUILayout.Space(10);

            // === Условия ===
            _conditionsList.DoLayoutList();

            EditorGUILayout.Space(5);

            // === Настройки ===
            EditorGUILayout.PropertyField(serializedObject.FindProperty("canBeUnsolved"),
                new GUIContent("Можно рассрешить"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("maxSolveCount"),
                new GUIContent("Макс. решений (0 = ∞)"));

            EditorGUILayout.Space(10);

            // === События ===
            EditorGUILayout.LabelField("События", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("OnPuzzleSolved"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("OnPuzzleUnsolved"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("OnProgressChanged"));

            EditorGUILayout.Space(10);

            // === Детальный статус ===
            _showDetailedStatus = EditorGUILayout.Foldout(_showDetailedStatus, "📋 Детальный статус");
            if (_showDetailedStatus)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(controller.GetDetailedStatus(), EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndVertical();
            }

            // === Быстрые действия ===
            EditorGUILayout.Space(5);
            if (Application.isPlaying)
            {
                if (GUILayout.Button("🔄 Пересчитать условия", GUILayout.Height(25)))
                {
                    controller.ForceCheck();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}