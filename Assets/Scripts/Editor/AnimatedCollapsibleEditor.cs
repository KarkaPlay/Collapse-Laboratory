using Objects;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(AnimatedCollapsible))]
    public class AnimatedCollapsibleEditor : UnityEditor.Editor
    {
        private SerializedProperty _animatorProperty;
        private SerializedProperty _animationSpeedProperty;
        private SerializedProperty _startAnimationProgressProperty;

        private void OnEnable()
        {
            _animatorProperty = serializedObject.FindProperty("animator");
            _animationSpeedProperty = serializedObject.FindProperty("animationSpeed");
            _startAnimationProgressProperty = serializedObject.FindProperty("startAnimationProgress");
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
            EditorGUILayout.LabelField("Скорость анимации", EditorStyles.miniBoldLabel);
            EditorGUILayout.HelpBox("Множитель скорости анимации. Значение 1 = нормальная скорость", MessageType.None);
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
                "• Взаимодействие с UI слайдером",
                MessageType.None);

            serializedObject.ApplyModifiedProperties();
        }
    }
}