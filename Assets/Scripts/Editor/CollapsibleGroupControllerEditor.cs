using UnityEditor;
using UnityEngine;

namespace CLEditor
{
    [CustomEditor(typeof(CollapsibleGroupController))]
    public class CollapsibleGroupControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            CollapsibleGroupController controller = (CollapsibleGroupController)target;
            serializedObject.Update();

            // === Заголовок ===
            GUI.backgroundColor = new Color(1f, 0.7f, 0.3f);
            GUIStyle headerStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 8, 8)
            };
            EditorGUILayout.LabelField("⚡ ГРУППА НЕСТАБИЛЬНЫХ ОБЪЕКТОВ", headerStyle);
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(5);

            // === Объекты ===
            EditorGUILayout.LabelField("Объекты группы", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("collapsibles"), true);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Найти Collapsible в дочерних", GUILayout.Height(22)))
            {
                controller.SetCollapsiblesFromChildren();
                EditorUtility.SetDirty(controller);
            }

            if (GUILayout.Button("Preview Pattern", GUILayout.Height(22)))
            {
                PatternPreviewWindow.ShowWindow();
                Selection.activeObject = controller;
            }

            EditorGUILayout.EndHorizontal();

            if (controller.Collapsibles.Count == 0)
            {
                EditorGUILayout.HelpBox("Нет объектов! Добавьте вручную или нажмите кнопку выше.",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(10);

            // === Паттерн ===
            EditorGUILayout.LabelField("Паттерн нестабильности", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("pattern"));

            // Описание
            string patternDesc = controller.pattern switch
            {
                InstabilityPattern.Synchronized =>
                    "✓ Все объекты переключаются одновременно\n  Ритмичный синхронный пульс",
                InstabilityPattern.Sequential =>
                    "✓ Объекты переключаются по очереди\n  Волна проходит последовательно",
                InstabilityPattern.Random => "✓ Случайный порядок и интервал\n  Хаотичная непредсказуемость",
                InstabilityPattern.Wave => "✓ Волна от первого к последнему\n  Направленная волна изменений",
                InstabilityPattern.Accelerating =>
                    "✓ Синхронное переключение с ускорением\n  Интервал уменьшается каждый цикл",
                InstabilityPattern.PingPong => "✓ Волна туда-обратно\n  Осциллирующее движение",
                InstabilityPattern.Radial => "✓ От центра к краям\n  Радиальное распространение",
                InstabilityPattern.Clustered =>
                    "✓ Группами (кластерами)\n  Несколько объектов одновременно",
                InstabilityPattern.Domino =>
                    "✓ Эффект домино с ускорением\n  Каскад с нарастающей скоростью",
                InstabilityPattern.Custom => "✓ Пользовательская последовательность\n  Задайте порядок вручную",
                _ => ""
            };
            EditorGUILayout.HelpBox(patternDesc, MessageType.Info);

            EditorGUILayout.Space(5);

            // === Timing ===
            EditorGUILayout.LabelField("Timing", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("switchStateInterval"),
                new GUIContent("Интервал (сек)"));

            // Показывать специфичные параметры паттерна
            if (controller.pattern == InstabilityPattern.Sequential ||
                controller.pattern == InstabilityPattern.Wave ||
                controller.pattern == InstabilityPattern.PingPong ||
                controller.pattern == InstabilityPattern.Random ||
                controller.pattern == InstabilityPattern.Radial ||
                controller.pattern == InstabilityPattern.Clustered ||
                controller.pattern == InstabilityPattern.Domino)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("delayBetweenObjects"),
                    new GUIContent("Задержка между объектами"));
            }

            if (controller.pattern == InstabilityPattern.Accelerating)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("minInterval"),
                    new GUIContent("Мин. интервал"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("accelerationRate"),
                    new GUIContent("Скорость ускорения"));
            }

            if (controller.pattern == InstabilityPattern.Random)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("randomIntervalVariance"),
                    new GUIContent("Разброс интервала (±)"));
            }

            if (controller.pattern == InstabilityPattern.Clustered)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("clusterSize"),
                    new GUIContent("Размер кластера"));
            }

            if (controller.pattern == InstabilityPattern.Custom)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("customSequence"), true);
                EditorGUILayout.HelpBox(
                    "Укажите индексы объектов в нужном порядке.\nНапример: 0, 2, 1, 3 для переключения в этой последовательности.",
                    MessageType.Info);
            }

            EditorGUILayout.Space(10);

            // === Визуальная обратная связь ===
            EditorGUILayout.LabelField("Визуальная обратная связь", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("showWarningEffect"),
                new GUIContent("Предупреждающий эффект"));

            if (controller.showWarningEffect)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("warningTime"),
                    new GUIContent("Время предупреждения"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("warningColor"),
                    new GUIContent("Цвет предупреждения"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // === Звук ===
            EditorGUILayout.LabelField("Звуковая обратная связь", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("collapseSound"),
                new GUIContent("Звук переключения"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("warningSound"),
                new GUIContent("Звук предупреждения"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("audioSource"),
                new GUIContent("Audio Source"));

            EditorGUILayout.Space(10);

            // === События ===
            EditorGUILayout.LabelField("События", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("OnCycleStart"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("OnCycleComplete"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("OnIntervalChanged"));

            EditorGUILayout.Space(10);

            // === Управление ===
            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            EditorGUILayout.LabelField("Управление", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = controller.IsActive ? new Color(1f, 0.5f, 0.5f) : new Color(0.5f, 1f, 0.5f);
            if (GUILayout.Button(controller.IsActive ? "■ Остановить" : "▶ Запустить", GUILayout.Height(30)))
            {
                if (controller.IsActive)
                    controller.StopDynamicStateSwitching();
                else
                    controller.StartDynamicStateSwitching();
            }

            GUI.backgroundColor = new Color(0.7f, 0.9f, 1f);
            if (GUILayout.Button("⚡ Триггер сейчас", GUILayout.Height(30)))
            {
                controller.TriggerCycleNow();
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("↺ Reset всех объектов", GUILayout.Height(25)))
            {
                controller.ResetAllToInitial();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();
        }
    }
}