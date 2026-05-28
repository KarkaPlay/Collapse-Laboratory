#if UNITY_EDITOR
using LabBuilder.Builder;
using LabBuilder.Data;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LabBuilder.Editor
{
    public sealed class LabBuilderWindow : EditorWindow
    {
        // ──────────────────────────────────────
        //  State
        // ──────────────────────────────────────

        private enum Tab { Room, Settings, SceneStats }
        private Tab _currentTab = Tab.Room;
        private Vector2 _scrollPos;

        // Room Creation/Edit
        private RoomData _roomData = new();
        private Vector3 _roomPosition;
        private float _roomRotationY;
        private bool _showDoorsFoldout = true;
        private bool _showMaterialsFoldout = true;
        private bool _previewRoom = true;

        // Connection Mode
        private ConnectionMode _connectionMode = ConnectionMode.None;

        // Connection
        private LabRoomComponent _targetRoom;
        private int _targetDoorIndex;
        private float _corridorLength = 3f;
        private int _newRoomConnectDoorIndex = 0;

        // Editing existing room
        private LabRoomComponent _editingRoom;
        private bool _isEditMode;

        // Interactive positioning
        private bool _interactiveMode = true;

        // Styles
        private GUIStyle _headerStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _buttonActiveStyle;
        private GUIStyle _buttonInactiveStyle;
        private bool _stylesInitialized;

        // ──────────────────────────────────────
        //  Menu
        // ──────────────────────────────────────

        [MenuItem("Tools/Lab Builder %#L")]
        public static void OpenWindow()
        {
            var window = GetWindow<LabBuilderWindow>("Lab Builder");
            window.minSize = new Vector2(480, 650);
            window.Show();
        }

        // ──────────────────────────────────────
        //  Lifecycle
        // ──────────────────────────────────────

        private void OnEnable()
        {
            _roomData.ApplyDefaultDimensions();
            _roomData.ApplyDefaultMaterials();

            var settings = LabBuilderSettings.Instance;
            if (settings != null)
                _corridorLength = settings.DefaultConnectionLength;

            SceneView.duringSceneGui += OnSceneGUI;
            Selection.selectionChanged += OnSelectionChanged;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnSelectionChanged()
        {
            if (Selection.activeGameObject != null)
            {
                var room = Selection.activeGameObject.GetComponent<LabRoomComponent>();
                if (room != null && room != _editingRoom)
                {
                    StartEditingRoom(room);
                    Repaint();
                }
            }
        }

        // ──────────────────────────────────────
        //  Styles
        // ──────────────────────────────────────

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.9f, 0.9f, 0.95f) }
            };

            _sectionStyle = new GUIStyle("helpbox")
            {
                padding = new RectOffset(12, 12, 10, 10)
            };

            _buttonActiveStyle = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.3f, 0.85f, 0.45f) }
            };

            _buttonInactiveStyle = GUI.skin.button;

            _stylesInitialized = true;
        }

        // ──────────────────────────────────────
        //  Main GUI
        // ──────────────────────────────────────

        private void OnGUI()
        {
            InitStyles();

            DrawHeader();
            DrawTabs();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            EditorGUILayout.Space(8);

            switch (_currentTab)
            {
                case Tab.Room: DrawRoomTab(); break;
                case Tab.Settings: DrawSettingsTab(); break;
                case Tab.SceneStats: DrawSceneStatsTab(); break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(8);

            var headerRect = EditorGUILayout.GetControlRect(false, 35);
            EditorGUI.DrawRect(headerRect, new Color(0.15f, 0.15f, 0.2f));

            string headerText = _isEditMode
                ? $"✏️ РЕДАКТИРОВАНИЕ: {(_editingRoom != null ? _editingRoom.gameObject.name : "...")}"
                : "🔬 LAB ROOM BUILDER";

            GUI.Label(headerRect, headerText, _headerStyle);

            EditorGUILayout.Space(2);
        }

        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            DrawTabButton("🏗️ Комната", Tab.Room);
            DrawTabButton("⚙️ Настройки", Tab.Settings);
            DrawTabButton("📊 Статистика", Tab.SceneStats);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTabButton(string label, Tab tab)
        {
            bool isActive = _currentTab == tab;
            var style = isActive
                ? new GUIStyle(EditorStyles.toolbarButton) { fontStyle = FontStyle.Bold }
                : EditorStyles.toolbarButton;

            if (GUILayout.Toggle(isActive, label, style) && !isActive)
            {
                _currentTab = tab;
                GUI.FocusControl(null);
            }
        }

        // ══════════════════════════════════════
        //  Room Tab
        // ══════════════════════════════════════

        private void DrawRoomTab()
        {
            // Режим редактирования / создания
            if (_isEditMode)
            {
                DrawEditModeHeader();
            }
            else
            {
                DrawCreateModeHeader();
            }

            EditorGUILayout.Space(6);

            // Основные параметры комнаты
            DrawRoomParameters();

            EditorGUILayout.Space(6);

            // Двери
            DrawDoorsSection(_roomData, ref _showDoorsFoldout);

            EditorGUILayout.Space(6);

            // Материалы
            DrawMaterialsSection(_roomData, ref _showMaterialsFoldout);

            EditorGUILayout.Space(8);

            // Режим создания: позиция и соединение
            if (!_isEditMode)
            {
                DrawConnectionModeSelection();
                EditorGUILayout.Space(6);
                DrawPositionSettings();
                EditorGUILayout.Space(8);
            }

            // Валидация и кнопки действий
            DrawValidationAndActions();
        }

        // ──────────────────────────────────────
        //  Edit/Create Mode Headers
        // ──────────────────────────────────────

        private void DrawEditModeHeader()
        {
            EditorGUILayout.BeginVertical(_sectionStyle);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Редактирование комнаты", EditorStyles.boldLabel);

            if (GUILayout.Button("❌ Отменить", EditorStyles.miniButton, GUILayout.Width(80)))
            {
                CancelEditing();
                GUIUtility.ExitGUI();
                return;
            }
            EditorGUILayout.EndHorizontal();

            if (_editingRoom != null)
            {
                EditorGUILayout.LabelField($"Объект: {_editingRoom.gameObject.name}", EditorStyles.miniLabel);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "Измените параметры комнаты и нажмите 'Применить изменения' для обновления геометрии.",
                MessageType.Info
            );

            EditorGUILayout.EndVertical();
        }

        private void DrawCreateModeHeader()
        {
            EditorGUILayout.BeginVertical(_sectionStyle);

            EditorGUILayout.LabelField("Создание новой комнаты", EditorStyles.boldLabel);

            if (GUILayout.Button("📋 Создать из выбранной", EditorStyles.miniButton))
            {
                if (Selection.activeGameObject != null)
                {
                    var room = Selection.activeGameObject.GetComponent<LabRoomComponent>();
                    if (room != null)
                    {
                        _roomData.CopyFrom(room.Data);
                        Debug.Log($"[LabBuilder] Скопированы параметры из {room.gameObject.name}");
                    }
                }
            }

            EditorGUILayout.EndVertical();
        }

        // ──────────────────────────────────────
        //  Room Parameters
        // ──────────────────────────────────────

        private void DrawRoomParameters()
        {
            DrawSectionHeader("📐 Размеры комнаты");

            EditorGUILayout.BeginVertical(_sectionStyle);

            _roomData.width = Mathf.Max(2f, EditorGUILayout.FloatField("Ширина (м)", _roomData.width));
            _roomData.length = Mathf.Max(2f, EditorGUILayout.FloatField("Длина (м)", _roomData.length));
            _roomData.height = Mathf.Max(2.5f, EditorGUILayout.FloatField("Высота (м)", _roomData.height));
            _roomData.wallThickness = EditorGUILayout.Slider("Толщина стен", _roomData.wallThickness, 0.05f, 0.5f);

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("По умолчанию", EditorStyles.miniButton))
            {
                _roomData.ApplyDefaultDimensions();
            }
            if (GUILayout.Button("5×5", EditorStyles.miniButton))
            {
                _roomData.width = _roomData.length = 5f;
            }
            if (GUILayout.Button("8×8", EditorStyles.miniButton))
            {
                _roomData.width = _roomData.length = 8f;
            }
            if (GUILayout.Button("3×6", EditorStyles.miniButton))
            {
                _roomData.width = 3f;
                _roomData.length = 6f;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        // ──────────────────────────────────────
        //  Connection Mode Selection
        // ──────────────────────────────────────

        private void DrawConnectionModeSelection()
        {
            DrawSectionHeader("🔗 Режим создания");

            EditorGUILayout.BeginVertical(_sectionStyle);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Toggle(_connectionMode == ConnectionMode.None, "Отдельная комната",
                _connectionMode == ConnectionMode.None ? _buttonActiveStyle : _buttonInactiveStyle))
            {
                if (_connectionMode != ConnectionMode.None)
                {
                    _connectionMode = ConnectionMode.None;
                    _targetRoom = null;
                }
            }

            if (GUILayout.Toggle(_connectionMode == ConnectionMode.ConnectToExisting, "Подключить к двери",
                _connectionMode == ConnectionMode.ConnectToExisting ? _buttonActiveStyle : _buttonInactiveStyle))
            {
                if (_connectionMode != ConnectionMode.ConnectToExisting)
                {
                    _connectionMode = ConnectionMode.ConnectToExisting;
                }
            }

            EditorGUILayout.EndHorizontal();

            // Настройки подключения
            if (_connectionMode == ConnectionMode.ConnectToExisting)
            {
                EditorGUILayout.Space(6);

                _targetRoom = (LabRoomComponent)EditorGUILayout.ObjectField(
                    "Целевая комната", _targetRoom, typeof(LabRoomComponent), true
                );

                if (_targetRoom != null)
                {
                    if (_targetRoom.DoorCount == 0)
                    {
                        EditorGUILayout.HelpBox("У выбранной комнаты нет дверей!", MessageType.Warning);
                    }
                    else
                    {
                        var doorLabels = new string[_targetRoom.DoorCount];
                        for (int i = 0; i < doorLabels.Length; i++)
                        {
                            var d = _targetRoom.Data.doors[i];
                            var connected = _targetRoom.IsDoorConnected(i) ? " [ЗАНЯТА]" : "";
                            doorLabels[i] = $"Дверь {i}: {d.wall} ({d.width:F1}×{d.height:F1}м){connected}";
                        }

                        _targetDoorIndex = Mathf.Clamp(_targetDoorIndex, 0, doorLabels.Length - 1);
                        _targetDoorIndex = EditorGUILayout.Popup("Дверь исходной комнаты", _targetDoorIndex, doorLabels);

                        var info = _targetRoom.GetDoorWorldInfo(_targetDoorIndex);
                        EditorGUILayout.LabelField($"📍 {info.Position:F2}", EditorStyles.miniLabel);

                        if (_targetRoom.IsDoorConnected(_targetDoorIndex))
                        {
                            EditorGUILayout.HelpBox("⚠️ Эта дверь уже занята!", MessageType.Warning);
                        }

                        EditorGUILayout.Space(4);

                        var settings = LabBuilderSettings.Instance;
                        _corridorLength = EditorGUILayout.Slider(
                            "Длина коридора (м)",
                            _corridorLength,
                            settings?.MinConnectionLength ?? 1f,
                            settings?.MaxConnectionLength ?? 20f
                        );

                        EditorGUILayout.Space(6);

                        // === ВЫБОР ДВЕРИ НОВОЙ КОМНАТЫ ===
                        EditorGUILayout.LabelField("Дверь новой комнаты для подключения:", EditorStyles.boldLabel);

                        if (_roomData.doors.Count == 0)
                        {
                            EditorGUILayout.HelpBox(
                                "У новой комнаты нет дверей. Добавьте дверь ниже в секции 'Двери'.",
                                MessageType.Info
                            );
                            _newRoomConnectDoorIndex = 0;
                        }
                        else
                        {
                            var newRoomDoorLabels = new string[_roomData.doors.Count];
                            for (int i = 0; i < _roomData.doors.Count; i++)
                            {
                                var d = _roomData.doors[i];
                                newRoomDoorLabels[i] = $"Дверь {i}: {d.wall} (поз: {d.position:F2}, {d.width:F1}×{d.height:F1}м)";
                            }

                            _newRoomConnectDoorIndex = Mathf.Clamp(_newRoomConnectDoorIndex, 0, _roomData.doors.Count - 1);
                            _newRoomConnectDoorIndex = EditorGUILayout.Popup(
                                "Соединительная дверь",
                                _newRoomConnectDoorIndex,
                                newRoomDoorLabels
                            );

                            EditorGUILayout.HelpBox(
                                $"Комната будет размещена так, чтобы Дверь {_newRoomConnectDoorIndex} " +
                                $"({_roomData.doors[_newRoomConnectDoorIndex].wall}) соединилась с выбранной дверью через коридор.",
                                MessageType.None
                            );
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "Выберите комнату с дверью, к которой хотите подключить новую комнату.",
                        MessageType.Info
                    );
                }
            }

            EditorGUILayout.EndVertical();
        }

        // ──────────────────────────────────────
        //  Position Settings
        // ──────────────────────────────────────

        private void DrawPositionSettings()
        {
            DrawSectionHeader("📍 Размещение новой комнаты");

            EditorGUILayout.BeginVertical(_sectionStyle);

            if (_connectionMode == ConnectionMode.ConnectToExisting && _targetRoom != null && _targetRoom.DoorCount > 0)
            {
                EditorGUILayout.HelpBox(
                    "Комната автоматически размещается для выравнивания выбранных дверей.",
                    MessageType.Info
                );

                EditorGUILayout.Space(4);

                // Показываем вычисленную позицию
                if (_targetDoorIndex < _targetRoom.DoorCount)
                {
                    var doorInfo = _targetRoom.GetDoorWorldInfo(_targetDoorIndex);

                    // Используем выбранную пользователем дверь
                    int connectDoorIndex = _roomData.doors.Count > 0
                        ? Mathf.Clamp(_newRoomConnectDoorIndex, 0, _roomData.doors.Count - 1)
                        : 0;

                    var calculatedPos = LabGeometryBuilder.CalculateRoomPositionFromDoor(
                        doorInfo, _roomData, _corridorLength, connectDoorIndex
                    );

                    _roomPosition = calculatedPos.position;
                    _roomRotationY = calculatedPos.rotation.eulerAngles.y;

                    EditorGUILayout.LabelField($"Вычисленная позиция: {_roomPosition:F2}", EditorStyles.miniLabel);
                    EditorGUILayout.LabelField($"Поворот: {_roomRotationY:F1}°", EditorStyles.miniLabel);

                    if (_roomData.doors.Count > 0 && connectDoorIndex < _roomData.doors.Count)
                    {
                        var connDoor = _roomData.doors[connectDoorIndex];
                        EditorGUILayout.LabelField(
                            $"Подключаемая дверь: {connectDoorIndex} ({connDoor.wall}, поз: {connDoor.position:F2})",
                            EditorStyles.miniLabel
                        );
                    }
                }
            }
            else
            {
                // Ручное размещение
                _roomPosition = EditorGUILayout.Vector3Field("Позиция", _roomPosition);

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("🎥 Из SceneView", EditorStyles.miniButton))
                {
                    if (SceneView.lastActiveSceneView != null)
                    {
                        var cam = SceneView.lastActiveSceneView.camera;
                        _roomPosition = cam.transform.position + cam.transform.forward * 8f;
                        _roomPosition.y = 0f;
                    }
                }

                if (GUILayout.Button("📌 Сетка 5м", EditorStyles.miniButton))
                {
                    _roomPosition = new Vector3(
                        Mathf.Round(_roomPosition.x / 5f) * 5f,
                        0f,
                        Mathf.Round(_roomPosition.z / 5f) * 5f
                    );
                }

                if (GUILayout.Button("⭕ В ноль", EditorStyles.miniButton))
                {
                    _roomPosition = Vector3.zero;
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);

                _roomRotationY = EditorGUILayout.Slider("Поворот Y°", _roomRotationY, 0f, 360f);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("0°", EditorStyles.miniButton)) _roomRotationY = 0f;
                if (GUILayout.Button("90°", EditorStyles.miniButton)) _roomRotationY = 90f;
                if (GUILayout.Button("180°", EditorStyles.miniButton)) _roomRotationY = 180f;
                if (GUILayout.Button("270°", EditorStyles.miniButton)) _roomRotationY = 270f;
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(6);

            _previewRoom = EditorGUILayout.Toggle("Предпросмотр в сцене", _previewRoom);

            if (_connectionMode == ConnectionMode.None)
            {
                _interactiveMode = EditorGUILayout.Toggle("Интерактивное позиционирование", _interactiveMode);

                if (_interactiveMode)
                {
                    EditorGUILayout.HelpBox(
                        "🎮 Используйте стрелки в SceneView для перемещения комнаты",
                        MessageType.None
                    );
                }
            }

            EditorGUILayout.EndVertical();
        }

        // ──────────────────────────────────────
        //  Validation & Actions
        // ──────────────────────────────────────

        private void DrawValidationAndActions()
        {
            bool isValid = _roomData.Validate(out var errors);

            if (!isValid)
            {
                foreach (var err in errors)
                    EditorGUILayout.HelpBox(err, MessageType.Warning);
            }

            // Проверка пересечений (только для создания новой)
            bool hasOverlap = false;
            LabRoomComponent overlapping = null;

            if (!_isEditMode && _connectionMode == ConnectionMode.None)
            {
                var previewBounds = new Bounds(
                    _roomPosition + Vector3.up * _roomData.height * 0.5f,
                    new Vector3(_roomData.width, _roomData.height, _roomData.length)
                );

                hasOverlap = LabGeometryBuilder.CheckOverlap(previewBounds, out overlapping);

                if (hasOverlap)
                {
                    EditorGUILayout.HelpBox(
                        $"⚠️ Пересечение с: {overlapping.gameObject.name}",
                        MessageType.Error
                    );
                }
            }

            EditorGUILayout.Space(4);

            // Кнопки действий
            if (_isEditMode)
            {
                DrawEditModeActions(isValid);
            }
            else
            {
                DrawCreateModeActions(isValid, hasOverlap);
            }
        }

        private void DrawEditModeActions(bool isValid)
        {
            EditorGUILayout.BeginHorizontal();

            GUI.enabled = isValid;
            GUI.backgroundColor = new Color(0.3f, 0.85f, 0.45f);

            if (GUILayout.Button("✅ ПРИМЕНИТЬ ИЗМЕНЕНИЯ", GUILayout.Height(40)))
            {
                ApplyRoomChanges();
            }

            GUI.backgroundColor = Color.white;
            GUI.enabled = true;

            if (GUILayout.Button("❌", GUILayout.Width(50), GUILayout.Height(40)))
            {
                CancelEditing();
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCreateModeActions(bool isValid, bool hasOverlap)
        {
            bool canCreate = isValid && !hasOverlap;

            if (_connectionMode == ConnectionMode.ConnectToExisting)
            {
                canCreate = canCreate && _targetRoom != null &&
                           _targetRoom.DoorCount > 0 &&
                           !_targetRoom.IsDoorConnected(_targetDoorIndex);
            }

            GUI.enabled = canCreate;
            GUI.backgroundColor = new Color(0.3f, 0.85f, 0.45f);

            string buttonText = _connectionMode == ConnectionMode.ConnectToExisting
                ? "🔗 СОЗДАТЬ И ПОДКЛЮЧИТЬ"
                : "🔨 ПОСТРОИТЬ КОМНАТУ";

            if (GUILayout.Button(buttonText, GUILayout.Height(40)))
            {
                CreateRoom();
            }

            GUI.backgroundColor = Color.white;
            GUI.enabled = true;
        }

        // ══════════════════════════════════════
        //  Room Creation & Editing Logic
        // ══════════════════════════════════════

        private void CreateRoom()
        {
            GameObject newRoom;

            if (_connectionMode == ConnectionMode.ConnectToExisting && _targetRoom != null)
            {
                // Используем выбранную дверь
                int connectDoorIndex = _roomData.doors.Count > 0
                    ? Mathf.Clamp(_newRoomConnectDoorIndex, 0, _roomData.doors.Count - 1)
                    : 0;

                // Вычисляем финальную позицию
                var doorInfo = _targetRoom.GetDoorWorldInfo(_targetDoorIndex);
                var placement = LabGeometryBuilder.CalculateRoomPositionFromDoor(
                    doorInfo, _roomData, _corridorLength, connectDoorIndex
                );

                // Создаём комнату на вычисленной позиции
                newRoom = LabGeometryBuilder.BuildRoom(_roomData, placement.position, placement.rotation);

                if (newRoom != null)
                {
                    // Создаём коридор
                    var corridor = LabGeometryBuilder.CreateStraightCorridorPublic(
                        doorInfo.Position,
                        doorInfo.Forward,
                        _corridorLength,
                        doorInfo.Width,
                        doorInfo.Height,
                        _targetRoom.Data
                    );

                    if (corridor != null)
                    {
                        corridor.transform.SetParent(_targetRoom.transform);
                        corridor.name = $"Corridor_to_{newRoom.name}";
                    }

                    // Регистрируем соединение
                    Undo.RecordObject(_targetRoom, "Connect Room");
                    _targetRoom.connections.Add(new ConnectionInfo
                    {
                        sourceDoorIndex = _targetDoorIndex,
                        sourceDoorId = doorInfo.DoorId,
                        connectedObject = newRoom,
                        connectionType = ConnectionType.DirectRoom,
                        targetDoorIndex = connectDoorIndex,
                        connectionLength = _corridorLength
                    });

                    EditorUtility.SetDirty(_targetRoom);

                    Debug.Log($"[LabBuilder] Комната подключена. Коридор: {_corridorLength:F1}м. " +
                             $"Дверь {_targetDoorIndex} → Дверь {connectDoorIndex}");
                }
            }
            else
            {
                var rotation = Quaternion.Euler(0f, _roomRotationY, 0f);
                newRoom = LabGeometryBuilder.BuildRoom(_roomData, _roomPosition, rotation);
            }

            if (newRoom != null)
            {
                _roomData = new RoomData();
                _roomData.ApplyDefaultDimensions();
                _roomData.ApplyDefaultMaterials();
                _roomRotationY = 0f;
                _newRoomConnectDoorIndex = 0;

                Selection.activeGameObject = newRoom;
                SceneView.lastActiveSceneView?.FrameSelected();
            }
        }

        private void StartEditingRoom(LabRoomComponent room)
        {
            _editingRoom = room;
            _isEditMode = true;
            _roomData.CopyFrom(room.Data);
            _currentTab = Tab.Room;
        }

        private void ApplyRoomChanges()
        {
            if (_editingRoom == null) return;

            Undo.RecordObject(_editingRoom, "Modify Room");

            _editingRoom.roomData.CopyFrom(_roomData);
            EditorUtility.SetDirty(_editingRoom);

            LabGeometryBuilder.RebuildRoom(_editingRoom);

            Debug.Log($"[LabBuilder] Комната {_editingRoom.gameObject.name} обновлена");

            CancelEditing();
        }

        private void CancelEditing()
        {
            _editingRoom = null;
            _isEditMode = false;

            _roomData = new RoomData();
            _roomData.ApplyDefaultDimensions();
            _roomData.ApplyDefaultMaterials();
        }

        // ══════════════════════════════════════
        //  Reusable Components
        // ══════════════════════════════════════

        private void DrawSectionHeader(string text)
        {
            EditorGUILayout.Space(4);
            var rect = EditorGUILayout.GetControlRect(false, 24);
            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.25f));
            GUI.Label(rect, text, new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(10, 0, 0, 0)
            });
            EditorGUILayout.Space(2);
        }

        private void DrawDoorsSection(RoomData data, ref bool foldout)
        {
            foldout = EditorGUILayout.Foldout(foldout, $"🚪 Двери ({data.doors.Count})", true);

            if (!foldout) return;

            EditorGUI.indentLevel++;

            for (int i = 0; i < data.doors.Count; i++)
            {
                EditorGUILayout.BeginVertical(_sectionStyle);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Дверь {i}", EditorStyles.miniBoldLabel);

                if (GUILayout.Button("❌", EditorStyles.miniButton, GUILayout.Width(30)))
                {
                    data.doors.RemoveAt(i);
                    i--;
                    GUIUtility.ExitGUI();
                    continue;
                }
                EditorGUILayout.EndHorizontal();

                var door = data.doors[i];
                door.wall = (WallSide)EditorGUILayout.EnumPopup("Стена", door.wall);
                door.position = EditorGUILayout.Slider("Позиция", door.position, 0.15f, 0.85f);
                door.width = Mathf.Max(0.7f, EditorGUILayout.FloatField("Ширина", door.width));
                door.height = Mathf.Max(1.5f, EditorGUILayout.FloatField("Высота", door.height));

                float wallLen = door.wall is WallSide.North or WallSide.South ? data.width : data.length;

                if (!door.Validate(wallLen, data.height, out var doorErr))
                    EditorGUILayout.HelpBox(doorErr, MessageType.Warning);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            if (GUILayout.Button("➕ Добавить дверь"))
            {
                var settings = LabBuilderSettings.Instance;
                data.doors.Add(new DoorData
                {
                    width = settings?.DefaultDoorWidth ?? 1.2f,
                    height = settings?.DefaultDoorHeight ?? 2.4f
                });
            }

            EditorGUI.indentLevel--;
        }

        private void DrawMaterialsSection(RoomData data, ref bool foldout)
        {
            foldout = EditorGUILayout.Foldout(foldout, "🎨 Материалы", true);

            if (!foldout) return;

            EditorGUILayout.BeginVertical(_sectionStyle);

            data.floorMaterial = (Material)EditorGUILayout.ObjectField(
                "Пол", data.floorMaterial, typeof(Material), false
            );
            data.ceilingMaterial = (Material)EditorGUILayout.ObjectField(
                "Потолок", data.ceilingMaterial, typeof(Material), false
            );
            data.wallMaterial = (Material)EditorGUILayout.ObjectField(
                "Стены", data.wallMaterial, typeof(Material), false
            );

            EditorGUILayout.Space(4);

            if (GUILayout.Button("Применить материалы по умолчанию", EditorStyles.miniButton))
            {
                data.ApplyDefaultMaterials();
            }

            EditorGUILayout.EndVertical();
        }

        // ══════════════════════════════════════
        //  Settings Tab
        // ══════════════════════════════════════

        private void DrawSettingsTab()
        {
            var settings = LabBuilderSettings.Instance;

            if (settings == null)
            {
                EditorGUILayout.HelpBox("Настройки не найдены!", MessageType.Error);
                return;
            }

            DrawSectionHeader("⚙️ Глобальные настройки");

            var so = new SerializedObject(settings);
            so.Update();

            EditorGUILayout.BeginVertical(_sectionStyle);

            EditorGUILayout.LabelField("Материалы по умолчанию", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("_defaultFloorMaterial"));
            EditorGUILayout.PropertyField(so.FindProperty("_defaultCeilingMaterial"));
            EditorGUILayout.PropertyField(so.FindProperty("_defaultWallMaterial"));

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            EditorGUILayout.BeginVertical(_sectionStyle);

            EditorGUILayout.LabelField("Размеры по умолчанию", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("_defaultRoomWidth"));
            EditorGUILayout.PropertyField(so.FindProperty("_defaultRoomLength"));
            EditorGUILayout.PropertyField(so.FindProperty("_defaultRoomHeight"));
            EditorGUILayout.PropertyField(so.FindProperty("_defaultWallThickness"));

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            EditorGUILayout.BeginVertical(_sectionStyle);

            EditorGUILayout.LabelField("Двери", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("_defaultDoorWidth"));
            EditorGUILayout.PropertyField(so.FindProperty("_defaultDoorHeight"));

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            EditorGUILayout.BeginVertical(_sectionStyle);

            EditorGUILayout.LabelField("Соединения", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("_minConnectionLength"));
            EditorGUILayout.PropertyField(so.FindProperty("_defaultConnectionLength"));
            EditorGUILayout.PropertyField(so.FindProperty("_maxConnectionLength"));

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            EditorGUILayout.BeginVertical(_sectionStyle);

            EditorGUILayout.LabelField("Визуализация", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(so.FindProperty("_doorGizmoColor"));
            EditorGUILayout.PropertyField(so.FindProperty("_connectionPreviewColor"));
            EditorGUILayout.PropertyField(so.FindProperty("_showDoorLabels"));

            EditorGUILayout.EndVertical();

            so.ApplyModifiedProperties();

            EditorGUILayout.Space(8);

            if (GUILayout.Button("Открыть файл настроек"))
            {
                Selection.activeObject = settings;
                EditorGUIUtility.PingObject(settings);
            }
        }

        // ══════════════════════════════════════
        //  Scene Stats Tab
        // ══════════════════════════════════════

        private void DrawSceneStatsTab()
        {
            DrawSectionHeader("📊 Статистика сцены");

            var rooms = FindObjectsByType<LabRoomComponent>(FindObjectsSortMode.None);

            EditorGUILayout.BeginVertical(_sectionStyle);

            EditorGUILayout.LabelField($"Всего комнат: {rooms.Length}", EditorStyles.boldLabel);

            int totalDoors = 0;
            int connectedDoors = 0;
            float totalVolume = 0f;

            foreach (var room in rooms)
            {
                totalDoors += room.DoorCount;
                totalVolume += room.Data.width * room.Data.length * room.Data.height;

                foreach (var conn in room.Connections)
                {
                    if (conn.connectedObject != null)
                        connectedDoors++;
                }
            }

            EditorGUILayout.LabelField($"Всего дверей: {totalDoors}");
            EditorGUILayout.LabelField($"Подключено: {connectedDoors}");
            EditorGUILayout.LabelField($"Свободно: {totalDoors - connectedDoors}");
            EditorGUILayout.LabelField($"Общий объём: {totalVolume:F1} м³");

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            if (rooms.Length > 0)
            {
                EditorGUILayout.LabelField("Список комнат:", EditorStyles.boldLabel);

                foreach (var room in rooms)
                {
                    EditorGUILayout.BeginHorizontal(_sectionStyle);

                    EditorGUILayout.LabelField(room.gameObject.name, GUILayout.Width(200));
                    EditorGUILayout.LabelField(
                        $"{room.Data.width:F1}×{room.Data.length:F1}×{room.Data.height:F1}",
                        GUILayout.Width(100)
                    );

                    if (GUILayout.Button("Выбрать", EditorStyles.miniButton, GUILayout.Width(60)))
                    {
                        Selection.activeGameObject = room.gameObject;
                        SceneView.lastActiveSceneView?.FrameSelected();
                    }

                    if (GUILayout.Button("Редактировать", EditorStyles.miniButton, GUILayout.Width(100)))
                    {
                        StartEditingRoom(room);
                        _currentTab = Tab.Room;
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        // ══════════════════════════════════════
        //  Scene GUI (Interactive Positioning)
        // ══════════════════════════════════════

        private void OnSceneGUI(SceneView sceneView)
        {
            if (_currentTab != Tab.Room || !_previewRoom || _isEditMode)
                return;

            var settings = LabBuilderSettings.Instance;
            var previewColor = settings?.ConnectionPreviewColor ?? new Color(0.9f, 0.6f, 0.1f, 0.5f);

            // Вычисляем позицию и ротацию
            Vector3 roomPos = _roomPosition;
            Quaternion roomRot = Quaternion.Euler(0f, _roomRotationY, 0f);

            if (_connectionMode == ConnectionMode.ConnectToExisting &&
                _targetRoom != null &&
                _targetDoorIndex < _targetRoom.DoorCount)
            {
                DrawConnectionPreview();
            }
            else
            {
                // Отдельная комната - простой предпросмотр
                DrawStandaloneRoomPreview(roomPos, roomRot);
            }

            // Интерактивное позиционирование
            if (_interactiveMode && _connectionMode == ConnectionMode.None)
            {
                EditorGUI.BeginChangeCheck();

                Vector3 newPos = Handles.PositionHandle(roomPos, roomRot);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(this, "Move Room Preview");
                    _roomPosition = newPos;
                    Repaint();
                }
            }
        }

        /// <summary>
        /// Рисует превью отдельной комнаты (не подключённой).
        /// </summary>
        private void DrawStandaloneRoomPreview(Vector3 roomPos, Quaternion roomRot)
        {
            var settings = LabBuilderSettings.Instance;
            var previewColor = settings?.ConnectionPreviewColor ?? new Color(0.9f, 0.6f, 0.1f, 0.5f);

            var roomSize = new Vector3(_roomData.width, _roomData.height, _roomData.length);
            var roomCenter = roomPos + Vector3.up * _roomData.height * 0.5f;

            Handles.color = previewColor;
            Handles.matrix = Matrix4x4.TRS(roomCenter, roomRot, Vector3.one);
            Handles.DrawWireCube(Vector3.zero, roomSize);
            Handles.matrix = Matrix4x4.identity;

            // Рисуем двери
            DrawRoomDoorsPreview(roomPos, roomRot, _roomData, -1);
        }

        private void DrawConnectionPreview()
        {
            if (_targetRoom == null || _targetDoorIndex >= _targetRoom.DoorCount)
                return;

            var doorInfo = _targetRoom.GetDoorWorldInfo(_targetDoorIndex);
            var settings = LabBuilderSettings.Instance;

            // Highlight source door
            Handles.color = Color.yellow;
            var doorCenter = doorInfo.Position + Vector3.up * doorInfo.Height * 0.5f;
            Handles.DrawWireCube(doorCenter, new Vector3(doorInfo.Width, doorInfo.Height, 0.1f));

            if (_roomData.doors.Count == 0)
            {
                var placement = LabGeometryBuilder.CalculateRoomPositionFromDoor(
                    doorInfo, _roomData, _corridorLength, 0
                );

                var roomPos = placement.position;
                var roomRot = placement.rotation;

                _roomPosition = roomPos;
                _roomRotationY = roomRot.eulerAngles.y;

                var roomSize = new Vector3(_roomData.width, _roomData.height, _roomData.length);
                var roomCenter = roomPos + Vector3.up * _roomData.height * 0.5f;

                Handles.color = new Color(1f, 0.5f, 0f, 0.3f);
                Handles.matrix = Matrix4x4.TRS(roomCenter, roomRot, Vector3.one);
                Handles.DrawWireCube(Vector3.zero, roomSize);
                Handles.matrix = Matrix4x4.identity;

                Handles.color = new Color(0.2f, 0.9f, 0.3f, 0.6f);
                var corridorEnd = doorInfo.Position + doorInfo.Forward * _corridorLength;
                Handles.DrawLine(doorInfo.Position + Vector3.up, corridorEnd + Vector3.up, 4f);

                return;
            }

            // Используем выбранную пользователем дверь
            int connectDoorIndex = Mathf.Clamp(_newRoomConnectDoorIndex, 0, _roomData.doors.Count - 1);

            var roomPlacement = LabGeometryBuilder.CalculateRoomPositionFromDoor(
                doorInfo,
                _roomData,
                _corridorLength,
                connectDoorIndex
            );

            var finalRoomPos = roomPlacement.position;
            var finalRoomRot = roomPlacement.rotation;

            _roomPosition = finalRoomPos;
            _roomRotationY = finalRoomRot.eulerAngles.y;

            var finalRoomSize = new Vector3(_roomData.width, _roomData.height, _roomData.length);
            var finalRoomCenter = finalRoomPos + Vector3.up * _roomData.height * 0.5f;

            Handles.color = settings?.ConnectionPreviewColor ?? new Color(0.9f, 0.6f, 0.1f, 0.3f);

            Handles.matrix = Matrix4x4.TRS(finalRoomCenter, finalRoomRot, Vector3.one);
            Handles.DrawWireCube(Vector3.zero, finalRoomSize);
            Handles.matrix = Matrix4x4.identity;

            DrawRoomDoorsPreview(finalRoomPos, finalRoomRot, _roomData, connectDoorIndex);

            Handles.color = new Color(0.2f, 0.9f, 0.3f, 0.6f);
            var finalCorridorEnd = doorInfo.Position + doorInfo.Forward * _corridorLength;
            Handles.DrawLine(doorInfo.Position + Vector3.up, finalCorridorEnd + Vector3.up, 4f);

            Handles.ConeHandleCap(0,
                doorInfo.Position + doorInfo.Forward * (_corridorLength * 0.5f) + Vector3.up,
                Quaternion.LookRotation(doorInfo.Forward),
                0.5f,
                EventType.Repaint
            );
        }

        /// <summary>
        /// Находит лучшую дверь для подключения в новой комнате.
        /// Приоритет: South > North > East > West
        /// </summary>
        private int FindBestDoorForConnection()
        {
            if (_roomData.doors.Count == 0)
                return -1;

            if (_targetRoom == null || _targetDoorIndex >= _targetRoom.DoorCount)
                return 0;

            var sourceDoor = _targetRoom.GetDoorWorldInfo(_targetDoorIndex);

            // Используем метод из LabGeometryBuilder
            return LabGeometryBuilder.FindBestDoorForConnection(_roomData, sourceDoor);
        }

        /// <summary>
        /// Рисует двери на превью комнаты.
        /// </summary>
        private void DrawRoomDoorsPreview(Vector3 roomPos, Quaternion roomRot, RoomData data, int highlightDoorIndex)
        {
            var settings = LabBuilderSettings.Instance;

            for (int i = 0; i < data.doors.Count; i++)
            {
                var door = data.doors[i];

                // Вычисляем локальную позицию двери
                Vector3 localPos = LabRoomComponent.ComputeDoorLocalPosition(door, data);
                Vector3 localNormal = LabRoomComponent.GetWallOutwardNormal(door.wall);

                // Преобразуем в мировые координаты
                Vector3 worldPos = roomPos + roomRot * localPos;
                Vector3 worldNormal = roomRot * localNormal;
                Quaternion doorRot = roomRot * Quaternion.LookRotation(localNormal);

                // Цвет двери
                if (i == highlightDoorIndex)
                    Handles.color = Color.cyan; // Подключаемая дверь
                else
                    Handles.color = new Color(0.5f, 0.5f, 0.8f, 0.4f); // Остальные двери

                var doorCenter = worldPos + Vector3.up * door.height * 0.5f;

                Handles.matrix = Matrix4x4.TRS(doorCenter, doorRot, Vector3.one);
                Handles.DrawWireCube(Vector3.zero, new Vector3(door.width, door.height, 0.05f));
                Handles.matrix = Matrix4x4.identity;

                // Стрелка направления
                Handles.DrawLine(worldPos + Vector3.up, worldPos + Vector3.up + worldNormal * 0.8f);

                // Метка
                if (settings != null && settings.ShowDoorLabels)
                {
                    var labelPos = worldPos + Vector3.up * door.height + worldNormal * 0.3f;
                    string label = i == highlightDoorIndex
                        ? $"Door {i}\n{door.wall}\n[CONNECT]"
                        : $"Door {i}\n{door.wall}";

                    Handles.Label(labelPos, label, new GUIStyle
                    {
                        normal = { textColor = i == highlightDoorIndex ? Color.cyan : Color.white },
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 10
                    });
                }
            }
        }
    }
}
#endif