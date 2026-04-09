using UnityEditor;
using UnityEngine;

namespace CLEditor
{
    [CustomEditor(typeof(Collapsible))]
    public class CollapsibleEditor : Editor
    {
        // Цвета для уровней стабильности
        private static readonly Color AbsoluteColor = new Color(0.6f, 0.6f, 0.6f);
        private static readonly Color StrongColor = new Color(1f, 0.85f, 0.3f);
        private static readonly Color WeakColor = new Color(0.4f, 0.7f, 1f);
        private static readonly Color UnstableColor = new Color(1f, 0.4f, 0.4f);

        public override void OnInspectorGUI()
        {
            Collapsible collapsible = (Collapsible)target;
            serializedObject.Update();

            // === Заголовок с цветовой индикацией ===
            DrawStabilityHeader(collapsible);

            EditorGUILayout.Space(5);

            // === Объекты состояний ===
            EditorGUILayout.LabelField("Объекты состояний", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stateOld"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stateNew"));

            if (GUILayout.Button("Найти COStates в дочерних", GUILayout.Height(22)))
            {
                collapsible.SetCOStatesFromChildren();
                EditorUtility.SetDirty(collapsible);
            }

            if (collapsible.stateOld == null || collapsible.stateNew == null)
            {
                EditorGUILayout.HelpBox(
                    "Один или оба объекта состояний не назначены!",
                    MessageType.Warning);
            }

            EditorGUILayout.Space(10);

            // === Стабильность ===
            EditorGUILayout.LabelField("Стабильность", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stabilityLevel"));

            // Информационная карточка о выбранном уровне
            DrawStabilityInfoCard(collapsible.stabilityLevel);

            EditorGUILayout.Space(5);

            // === Состояние ===
            EditorGUILayout.LabelField("Состояние", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("initialState"));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Текущее состояние");
            string stateIcon = collapsible.CurrentState == CollapseState.Old ? "🕰" : "🔬";
            EditorGUILayout.LabelField($"{stateIcon} {collapsible.CurrentState}");
            EditorGUILayout.EndHorizontal();

            // === Быстрые действия ===
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Быстрые действия", EditorStyles.boldLabel);
            using (new GUILayout.HorizontalScope())
            {
                GUI.backgroundColor = new Color(0.7f, 0.9f, 1f);
                if (GUILayout.Button("⟳ Collapse (Toggle)", GUILayout.Height(25)))
                {
                    collapsible.Collapse(CollapseOrigin.Script);
                }

                GUI.backgroundColor = new Color(1f, 0.9f, 0.7f);
                if (GUILayout.Button("↺ Reset", GUILayout.Height(25)))
                {
                    collapsible.Reset();
                }

                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.Space(10);

            // === Свойства (только для чтения) ===
            EditorGUILayout.LabelField("Разрешения (автоматически)", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle("Игрок может схлопнуть", collapsible.CanPlayerCollapse);
                EditorGUILayout.Toggle("Динамический (таймер)", collapsible.IsDynamic);
                EditorGUILayout.Toggle("Цель для связей", collapsible.CanBeLinkedTarget);
                EditorGUILayout.Toggle("Можно изменить", collapsible.CanBeChanged);
            }

            EditorGUILayout.Space(10);

            // === События ===
            EditorGUILayout.LabelField("События", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("OnCollapse"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("OnCollapseToOld"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("OnCollapseToNew"));

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawStabilityHeader(Collapsible collapsible)
        {
            Color headerColor = collapsible.stabilityLevel switch
            {
                StabilityLevel.Absolute => AbsoluteColor,
                StabilityLevel.Strong => StrongColor,
                StabilityLevel.Weak => WeakColor,
                StabilityLevel.Unstable => UnstableColor,
                _ => Color.white
            };

            string icon = collapsible.stabilityLevel switch
            {
                StabilityLevel.Absolute => "🔒",
                StabilityLevel.Strong => "🔗",
                StabilityLevel.Weak => "✋",
                StabilityLevel.Unstable => "⚡",
                _ => "?"
            };

            Color oldBg = GUI.backgroundColor;
            GUI.backgroundColor = headerColor;

            GUIStyle headerStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(10, 10, 8, 8)
            };

            EditorGUILayout.LabelField(
                $"{icon} COLLAPSIBLE — {collapsible.stabilityLevel.ToString().ToUpper()}",
                headerStyle);

            GUI.backgroundColor = oldBg;
        }

        private void DrawStabilityInfoCard(StabilityLevel level)
        {
            string info = level switch
            {
                StabilityLevel.Absolute =>
                    "Объект невозможно изменить. Определяет границы пространства.\n" +
                    "Нарратив: необратимый факт.",
                StabilityLevel.Strong =>
                    "Игрок НЕ может переключить напрямую. Только через связь (CollapseLinkController).\n" +
                    "Нарратив: скрытая связь, нужно найти причину.",
                StabilityLevel.Weak =>
                    "Игрок может свободно переключать нажатием [F].\n" +
                    "Нарратив: открытый вопрос, приглашение к исследованию.",
                StabilityLevel.Unstable =>
                    "Переключается автоматически по таймеру. Игрок тоже может переключать.\n" +
                    "Нарратив: мир рушится, аномалия берёт верх.",
                _ => ""
            };

            MessageType msgType = level switch
            {
                StabilityLevel.Absolute => MessageType.Info,
                StabilityLevel.Strong => MessageType.Warning,
                StabilityLevel.Weak => MessageType.Info,
                StabilityLevel.Unstable => MessageType.Error,
                _ => MessageType.None
            };

            EditorGUILayout.HelpBox(info, msgType);
        }
    }
}