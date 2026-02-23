using Objects;
using UnityEditor;
using UnityEngine;

namespace CLEditor
{
    [CustomEditor(typeof(AnimatedCollapsible))]
    public class AnimatedCollapsibleEditor : Editor
    {
        private SerializedProperty _animatorProperty;
        private SerializedProperty _animationSpeedProperty;
        private SerializedProperty _startAnimationProgressProperty;

        // Автоматическая анимация
        private SerializedProperty _isAutomaticAnimationProperty;
        private SerializedProperty _automaticAnimationSpeedProperty;
        private SerializedProperty _automaticAnimationDirectionProperty;

        private void OnEnable()
        {
            _animatorProperty = serializedObject.FindProperty("animator");
            _animationSpeedProperty = serializedObject.FindProperty("animationSpeed");
            _startAnimationProgressProperty = serializedObject.FindProperty("startAnimationProgress");

            _isAutomaticAnimationProperty = serializedObject.FindProperty("isAutomaticAnimation");
            _automaticAnimationSpeedProperty = serializedObject.FindProperty("automaticAnimationSpeed");
            _automaticAnimationDirectionProperty = serializedObject.FindProperty("automaticAnimationDirection");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Заголовок
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Настройки Animated Collapsible", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Секция: Аниматор
            EditorGUILayout.LabelField("Аниматор", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Укажите Animator компонент, который управляет анимацией объекта", MessageType.Info);
            EditorGUILayout.PropertyField(_animatorProperty);
            EditorGUILayout.Space(10);

            // Секция: Настройки анимации
            EditorGUILayout.LabelField("Настройки анимации", EditorStyles.boldLabel);

            // Animation Speed
            EditorGUILayout.LabelField("Скорость анимации (ручное управление)", EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox("Множитель скорости анимации при управлении игроком. Значение 1 = нормальная скорость", MessageType.None);
            EditorGUILayout.PropertyField(_animationSpeedProperty);

            // Start Animation Progress
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Начальный прогресс анимации", EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox("Начальное состояние анимации при старте игры (от 0 до 0.999)", MessageType.None);
            EditorGUILayout.Slider(_startAnimationProgressProperty, 0f, 0.999f, GUIContent.none);

            // Визуальное отображение прогресса
            EditorGUILayout.Space(5);
            float progress = _startAnimationProgressProperty.floatValue;
            Rect rect = GUILayoutUtility.GetRect(200, 20);
            EditorGUI.ProgressBar(rect, progress, $"Начальный прогресс: {progress:F3}");

            EditorGUILayout.Space(15);

            // Секция: Автоматическая анимация
            EditorGUILayout.LabelField("Автоматическая анимация", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Автоматическая анимация работает сама по себе, но приостанавливается, когда игрок берёт управление на себя.\n" +
                "После завершения взаимодействия автоматическая анимация возобновляется.",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_isAutomaticAnimationProperty, new GUIContent("Включить автоматическую анимацию"));
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(target);
            }

            if (_isAutomaticAnimationProperty.boolValue)
            {
                EditorGUILayout.Space(5);
                EditorGUI.indentLevel++;

                // Скорость автоматической анимации
                EditorGUILayout.LabelField("Скорость автоматической анимации", EditorStyles.miniBoldLabel);
                EditorGUILayout.HelpBox("Скорость автоматического воспроизведения анимации", MessageType.None);
                EditorGUILayout.PropertyField(_automaticAnimationSpeedProperty);

                // Направление автоматической анимации
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Направление", EditorStyles.miniBoldLabel);
                EditorGUILayout.HelpBox("1 = вперёд, -1 = назад", MessageType.None);

                float direction = _automaticAnimationDirectionProperty.floatValue;
                int directionIndex = direction >= 0 ? 0 : 1;
                string[] directionOptions = new string[] { "Вперёд (1)", "Назад (-1)" };
                int newIndex = EditorGUILayout.Popup("Направление", directionIndex, directionOptions);
                _automaticAnimationDirectionProperty.floatValue = newIndex == 0 ? 1f : -1f;

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(15);

            // Кнопка автонастройки
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

            // Подсказка
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox(
                "После настройки проверьте дочерние объекты:\n" +
                "1. У каждого должен быть компонент AnimatedCollapsibleChild\n" +
                "2. У каждого должен быть коллайдер",
                MessageType.Warning);

            // Разделитель
            EditorGUILayout.Space(15);
            GUILayout.Box("", GUILayout.Height(1), GUILayout.ExpandWidth(true));
            EditorGUILayout.Space(10);

            // Информация о классе
            EditorGUILayout.LabelField("Информация", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Класс AnimatedCollapsible управляет анимацией складных/раскладных объектов.\n\n" +
                "Основные функции:\n" +
                "• Управление прогрессом анимации\n" +
                "• Автоматическая настройка дочерних объектов\n" +
                "• Взаимодействие с UI слайдером\n" +
                "• Поддержка автоматической анимации с приоритетом ручного управления",
                MessageType.None);

            serializedObject.ApplyModifiedProperties();
        }
    }
}