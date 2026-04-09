#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CLEditor
{
    /// <summary>
    /// Окно предпросмотра паттернов нестабильности.
    /// Tools → Collapse Lab → Preview Patterns
    /// </summary>
    public class PatternPreviewWindow : EditorWindow
    {
        private CollapsibleGroupController _selectedController;
        private InstabilityPattern _previewPattern;
        private float _animationTime = 0f;
        private bool _isPlaying = false;
        private List<float> _objectTimings = new();

        [MenuItem("Tools/Collapse Lab/Preview Instability Patterns", false, 50)]
        public static void ShowWindow()
        {
            var window = GetWindow<PatternPreviewWindow>("Pattern Preview");
            window.minSize = new Vector2(400, 500);
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            _isPlaying = false;
        }

        private void OnEditorUpdate()
        {
            if (_isPlaying)
            {
                _animationTime += (float)EditorApplication.timeSinceStartup * 0.01f;
                Repaint();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("🎬 Предпросмотр паттернов", titleStyle);

            EditorGUILayout.Space(15);

            // Выбор контроллера
            EditorGUILayout.LabelField("Выберите контроллер группы:", EditorStyles.boldLabel);
            _selectedController = (CollapsibleGroupController)EditorGUILayout.ObjectField(
                "Group Controller",
                _selectedController,
                typeof(CollapsibleGroupController),
                true);

            if (_selectedController == null)
            {
                EditorGUILayout.HelpBox("Выберите CollapsibleGroupController из сцены для предпросмотра.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.Space(10);

            // Выбор паттерна
            EditorGUILayout.LabelField("Паттерн для предпросмотра:", EditorStyles.boldLabel);
            _previewPattern = (InstabilityPattern)EditorGUILayout.EnumPopup("Pattern", _previewPattern);

            EditorGUILayout.Space(10);

            // Кнопки управления
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = _isPlaying ? Color.red : Color.green;
            if (GUILayout.Button(_isPlaying ? "■ Остановить" : "▶ Воспроизвести", GUILayout.Height(30)))
            {
                _isPlaying = !_isPlaying;
                if (_isPlaying)
                {
                    CalculateTimings();
                    _animationTime = 0f;
                }
            }

            GUI.backgroundColor = Color.white;

            if (GUILayout.Button("↺ Сбросить", GUILayout.Height(30)))
            {
                _animationTime = 0f;
                _isPlaying = false;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(15);

            // Визуализация
            DrawVisualization();

            EditorGUILayout.Space(10);

            // Описание паттерна
            DrawPatternDescription();
        }

        private void CalculateTimings()
        {
            if (_selectedController == null || _selectedController.Collapsibles.Count == 0)
                return;

            _objectTimings.Clear();
            int count = _selectedController.Collapsibles.Count;
            float delay = _selectedController.delayBetweenObjects;

            switch (_previewPattern)
            {
                case InstabilityPattern.Synchronized:
                    for (int i = 0; i < count; i++)
                        _objectTimings.Add(0f);
                    break;

                case InstabilityPattern.Sequential:
                case InstabilityPattern.Wave:
                    for (int i = 0; i < count; i++)
                        _objectTimings.Add(i * delay);
                    break;

                case InstabilityPattern.Random:
                    var random = Enumerable.Range(0, count).OrderBy(_ => Random.value).ToList();
                    for (int i = 0; i < count; i++)
                        _objectTimings.Add(random[i] * delay);
                    break;

                case InstabilityPattern.Radial:
                    // Упрощённо: от центра
                    for (int i = 0; i < count; i++)
                    {
                        float normalized = Mathf.Abs((float)i / count - 0.5f) * 2f;
                        _objectTimings.Add(normalized * count * delay);
                    }

                    break;

                case InstabilityPattern.Clustered:
                    int clusterSize = _selectedController.clusterSize;
                    for (int i = 0; i < count; i++)
                    {
                        int cluster = i / clusterSize;
                        _objectTimings.Add(cluster * delay);
                    }

                    break;

                case InstabilityPattern.Domino:
                    float currentTime = 0f;
                    float currentDelay = delay;
                    for (int i = 0; i < count; i++)
                    {
                        _objectTimings.Add(currentTime);
                        currentTime += currentDelay;
                        currentDelay *= 0.9f;
                    }

                    break;

                default:
                    for (int i = 0; i < count; i++)
                        _objectTimings.Add(i * delay);
                    break;
            }
        }

        private void DrawVisualization()
        {
            if (_selectedController == null || _objectTimings.Count == 0)
                return;

            Rect visualRect = GUILayoutUtility.GetRect(0, 200, GUILayout.ExpandWidth(true));
            GUI.Box(visualRect, "", EditorStyles.helpBox);

            float maxTime = _objectTimings.Max() + 1f;
            int count = _objectTimings.Count;

            for (int i = 0; i < count; i++)
            {
                float timing = _objectTimings[i];
                float normalizedTime = timing / maxTime;

                float x = visualRect.x + 20 + normalizedTime * (visualRect.width - 40);
                float y = visualRect.y + 20 + (i / (float)count) * (visualRect.height - 40);

                // Цвет зависит от времени анимации
                float alpha = _isPlaying && _animationTime > timing && _animationTime < timing + 0.5f
                    ? Mathf.PingPong((_animationTime - timing) * 4f, 1f)
                    : 0.3f;

                Color color = new Color(1f, 0.5f, 0.2f, alpha);
                Rect objectRect = new Rect(x - 10, y - 10, 20, 20);

                EditorGUI.DrawRect(objectRect, color);
                GUI.Label(new Rect(x + 15, y - 7, 50, 20), $"{i}", EditorStyles.miniLabel);
            }

            // Временная шкала
            float timelineY = visualRect.y + visualRect.height - 10;
            EditorGUI.DrawRect(new Rect(visualRect.x + 20, timelineY, visualRect.width - 40, 2), Color.gray);

            // Текущее время
            if (_isPlaying)
            {
                float currentX = visualRect.x + 20 + (_animationTime / maxTime) * (visualRect.width - 40);
                EditorGUI.DrawRect(new Rect(currentX - 1, visualRect.y + 10, 2, visualRect.height - 20), Color.red);
            }
        }

        private void DrawPatternDescription()
        {
            string description = _previewPattern switch
            {
                InstabilityPattern.Synchronized =>
                    "Все объекты переключаются одновременно. Создаёт ощущение синхронного пульса.",
                InstabilityPattern.Sequential => "Объекты переключаются по очереди с равной задержкой.",
                InstabilityPattern.Random => "Случайный порядок переключения. Хаотичная нестабильность.",
                InstabilityPattern.Wave => "Волна проходит от первого объекта к последнему.",
                InstabilityPattern.Accelerating =>
                    "Как Synchronized, но интервал между циклами уменьшается.",
                InstabilityPattern.PingPong => "Волна движется туда-обратно.",
                InstabilityPattern.Radial => "Переключение распространяется от центра к краям.",
                InstabilityPattern.Clustered => "Объекты переключаются группами (кластерами).",
                InstabilityPattern.Domino => "Эффект домино с ускорением.",
                InstabilityPattern.Custom => "Пользовательская последовательность.",
                _ => ""
            };

            EditorGUILayout.HelpBox(description, MessageType.Info);
        }
    }
}
#endif