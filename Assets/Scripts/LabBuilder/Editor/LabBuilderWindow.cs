#if UNITY_EDITOR
using LabBuilder.Builder;
using LabBuilder.Data;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LabBuilder.Editor
{
    /// <summary>
    /// Окно редактора для создания модульных комнат и коридоров.
    /// Открывается: Tools → Lab Builder (Ctrl+Shift+L).
    /// Три вкладки: Комната, Коридор, Соединение.
    /// </summary>
    public sealed class LabBuilderWindow : EditorWindow
    {
        // ──────────────────────────────────────
        //  State
        // ──────────────────────────────────────

        private enum Tab { Room, Corridor, Connect }
        private Tab _currentTab = Tab.Room;
        private Vector2 _scrollPos;

        // Room
        private readonly RoomData _roomData = new();
        private Vector3 _roomPosition;
        private bool _showDoorsFoldout = true;

        // Corridor
        private readonly CorridorData _corridorData = new();
        private Vector3 _corridorPosition;
        private float _corridorRotationY;

        // Connect
        private LabRoomComponent _sourceRoom;
        private int _doorIndex;
        private readonly CorridorData _connectCorridorData = new();

        // Styles
        private GUIStyle _headerStyle;
        private GUIStyle _sectionStyle;
        private bool _stylesInitialized;

        // ──────────────────────────────────────
        //  Menu Item
        // ──────────────────────────────────────

        [MenuItem("Tools/Lab Builder %#L")]
        public static void OpenWindow()
        {
            var window = GetWindow<LabBuilderWindow>("Lab Builder");
            window.minSize = new Vector2(400, 550);
            window.Show();
        }

        // ──────────────────────────────────────
        //  Styles Init
        // ──────────────────────────────────────

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter
            };

            _sectionStyle = new GUIStyle("helpbox")
            {
                padding = new RectOffset(12, 12, 10, 10)
            };

            _stylesInitialized = true;
        }

        // ──────────────────────────────────────
        //  Main OnGUI
        // ──────────────────────────────────────

        private void OnGUI()
        {
            InitStyles();

            // ── Header ──
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField(
                "\U0001F52C Lab Room Builder", _headerStyle, GUILayout.Height(28));
            EditorGUILayout.LabelField(
                "Модульный конструктор лаборатории · ProBuilder",
                EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space(4);

            // ── Tabs ──
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            DrawTabButton("Комната", Tab.Room);
            DrawTabButton("Коридор", Tab.Corridor);
            DrawTabButton("Соединить", Tab.Connect);
            EditorGUILayout.EndHorizontal();

            // ── Content ──
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            EditorGUILayout.Space(8);

            switch (_currentTab)
            {
                case Tab.Room: DrawRoomTab(); break;
                case Tab.Corridor: DrawCorridorTab(); break;
                case Tab.Connect: DrawConnectTab(); break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawTabButton(string label, Tab tab)
        {
            bool isActive = _currentTab == tab;
            var style = isActive
                ? new GUIStyle(EditorStyles.toolbarButton) { fontStyle = FontStyle.Bold }
                : EditorStyles.toolbarButton;

            if (GUILayout.Toggle(isActive, label, style) && !isActive)
                _currentTab = tab;
        }

        // ══════════════════════════════════════
        //  Room Tab
        // ══════════════════════════════════════

        private void DrawRoomTab()
        {
            // ── Dimensions ──
            EditorGUILayout.LabelField("Размеры комнаты", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            EditorGUILayout.BeginVertical(_sectionStyle);
            {
                _roomData.width = Mathf.Max(2f,
                    EditorGUILayout.FloatField("Ширина (м)", _roomData.width));
                _roomData.length = Mathf.Max(2f,
                    EditorGUILayout.FloatField("Длина (м)", _roomData.length));
                _roomData.height = Mathf.Max(2.5f,
                    EditorGUILayout.FloatField("Высота (м)", _roomData.height));
                _roomData.wallThickness = EditorGUILayout.Slider(
                    "Толщина стен", _roomData.wallThickness, 0.05f, 0.5f);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            // ── Doors ──
            _showDoorsFoldout = EditorGUILayout.Foldout(
                _showDoorsFoldout,
                $"Двери ({_roomData.doors.Count})",
                toggleOnLabelClick: true);

            if (_showDoorsFoldout)
            {
                EditorGUI.indentLevel++;

                for (int i = 0; i < _roomData.doors.Count; i++)
                {
                    EditorGUILayout.BeginVertical(_sectionStyle);
                    {
                        EditorGUILayout.LabelField(
                            $"Дверь {i}", EditorStyles.miniBoldLabel);

                        var door = _roomData.doors[i];
                        door.wall = (WallSide)EditorGUILayout.EnumPopup("Стена", door.wall);
                        door.position = EditorGUILayout.Slider("Позиция", door.position, 0.15f, 0.85f);
                        door.width = Mathf.Max(0.7f,
                            EditorGUILayout.FloatField("Ширина", door.width));
                        door.height = Mathf.Max(1.5f,
                            EditorGUILayout.FloatField("Высота", door.height));

                        // Inline validation
                        float wallLen = door.wall is WallSide.North or WallSide.South
                            ? _roomData.width : _roomData.length;
                        if (!door.Validate(wallLen, _roomData.height, out var doorErr))
                            EditorGUILayout.HelpBox(doorErr, MessageType.Warning);

                        EditorGUILayout.Space(2);
                        if (GUILayout.Button("Удалить дверь", EditorStyles.miniButton))
                        {
                            _roomData.doors.RemoveAt(i);
                            i--;
                            GUIUtility.ExitGUI();
                        }
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }

                if (GUILayout.Button("+ Добавить дверь"))
                    _roomData.doors.Add(new DoorData());

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(8);

            // ── Materials ──
            EditorGUILayout.LabelField("Материалы", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(_sectionStyle);
            {
                _roomData.floorMaterial = (Material)EditorGUILayout.ObjectField(
                    "Пол", _roomData.floorMaterial, typeof(Material), false);
                _roomData.ceilingMaterial = (Material)EditorGUILayout.ObjectField(
                    "Потолок", _roomData.ceilingMaterial, typeof(Material), false);
                _roomData.wallMaterial = (Material)EditorGUILayout.ObjectField(
                    "Стены", _roomData.wallMaterial, typeof(Material), false);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            // ── Position ──
            _roomPosition = EditorGUILayout.Vector3Field("Позиция в сцене", _roomPosition);
            EditorGUILayout.Space(4);

            // ── Use Scene View position ──
            if (GUILayout.Button("Взять позицию из SceneView"))
            {
                if (SceneView.lastActiveSceneView != null)
                {
                    var cam = SceneView.lastActiveSceneView.camera;
                    _roomPosition = cam.transform.position +
                                    cam.transform.forward * 10f;
                    _roomPosition.y = 0f;
                }
            }

            EditorGUILayout.Space(8);

            // ── Validation ──
            if (!_roomData.Validate(out var errors))
            {
                foreach (var err in errors)
                    EditorGUILayout.HelpBox(err, MessageType.Warning);
            }

            // ── Overlap check ──
            var previewBounds = new Bounds(
                _roomPosition + Vector3.up * _roomData.height * 0.5f,
                new Vector3(_roomData.width, _roomData.height, _roomData.length));

            if (LabGeometryBuilder.CheckOverlap(previewBounds, out var overlapping))
            {
                EditorGUILayout.HelpBox(
                    $"Пересечение с: {overlapping.gameObject.name}",
                    MessageType.Error);
            }

            EditorGUILayout.Space(4);

            // ── Build Button ──
            GUI.backgroundColor = new Color(0.3f, 0.85f, 0.45f);
            if (GUILayout.Button("\U0001F528 Построить комнату", GUILayout.Height(38)))
            {
                LabGeometryBuilder.BuildRoom(_roomData, _roomPosition);
            }
            GUI.backgroundColor = Color.white;
        }

        // ══════════════════════════════════════
        //  Corridor Tab
        // ══════════════════════════════════════

        private void DrawCorridorTab()
        {
            EditorGUILayout.LabelField("Параметры коридора", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            EditorGUILayout.BeginVertical(_sectionStyle);
            {
                _corridorData.width = Mathf.Max(1.5f,
                    EditorGUILayout.FloatField("Ширина (м)", _corridorData.width));
                _corridorData.height = Mathf.Max(2.5f,
                    EditorGUILayout.FloatField("Высота (м)", _corridorData.height));
                _corridorData.length = Mathf.Max(1f,
                    EditorGUILayout.FloatField("Длина (м)", _corridorData.length));
                _corridorData.wallThickness = EditorGUILayout.Slider(
                    "Толщина стен", _corridorData.wallThickness, 0.05f, 0.5f);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            // Materials
            EditorGUILayout.LabelField("Материалы", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(_sectionStyle);
            {
                _corridorData.floorMaterial = (Material)EditorGUILayout.ObjectField(
                    "Пол", _corridorData.floorMaterial, typeof(Material), false);
                _corridorData.ceilingMaterial = (Material)EditorGUILayout.ObjectField(
                    "Потолок", _corridorData.ceilingMaterial, typeof(Material), false);
                _corridorData.wallMaterial = (Material)EditorGUILayout.ObjectField(
                    "Стены", _corridorData.wallMaterial, typeof(Material), false);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            // Position & Rotation
            _corridorPosition = EditorGUILayout.Vector3Field("Позиция", _corridorPosition);
            _corridorRotationY = EditorGUILayout.Slider(
                "Поворот Y°", _corridorRotationY, 0f, 360f);

            EditorGUILayout.Space(8);

            // Validation
            if (!_corridorData.Validate(out var errors))
            {
                foreach (var err in errors)
                    EditorGUILayout.HelpBox(err, MessageType.Warning);
            }

            // Build
            GUI.backgroundColor = new Color(0.35f, 0.55f, 0.95f);
            if (GUILayout.Button("\U0001F528 Построить коридор", GUILayout.Height(38)))
            {
                LabGeometryBuilder.BuildCorridor(
                    _corridorData,
                    _corridorPosition,
                    Quaternion.Euler(0f, _corridorRotationY, 0f));
            }
            GUI.backgroundColor = Color.white;
        }

        // ══════════════════════════════════════
        //  Connect Tab
        // ══════════════════════════════════════

        private void DrawConnectTab()
        {
            EditorGUILayout.LabelField("Соединить коридор с комнатой", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Выберите комнату (LabRoomComponent), укажите дверь — " +
                "коридор автоматически построится и выровняется по проёму.",
                MessageType.Info);
            EditorGUILayout.Space(6);

            // Room selection
            _sourceRoom = (LabRoomComponent)EditorGUILayout.ObjectField(
                "Комната", _sourceRoom, typeof(LabRoomComponent), allowSceneObjects: true);

            if (_sourceRoom == null)
            {
                EditorGUILayout.HelpBox(
                    "Перетащите сюда объект с компонентом LabRoomComponent",
                    MessageType.None);
                return;
            }

            if (_sourceRoom.DoorCount == 0)
            {
                EditorGUILayout.HelpBox(
                    "У выбранной комнаты нет дверей. " +
                    "Сначала создайте комнату с дверью.",
                    MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(4);

            // Door picker
            var doorLabels = new string[_sourceRoom.DoorCount];
            for (int i = 0; i < doorLabels.Length; i++)
            {
                var d = _sourceRoom.Data.doors[i];
                doorLabels[i] = $"Дверь {i}: {d.wall} (поз: {d.position:F2}, " +
                                $"{d.width:F1}×{d.height:F1}м)";
            }

            _doorIndex = Mathf.Clamp(_doorIndex, 0, doorLabels.Length - 1);
            _doorIndex = EditorGUILayout.Popup("Дверь", _doorIndex, doorLabels);

            // Show door world info
            var info = _sourceRoom.GetDoorWorldInfo(_doorIndex);
            EditorGUILayout.LabelField(
                $"Мировая позиция: {info.Position:F2}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField(
                $"Направление: {info.Forward:F2}", EditorStyles.miniLabel);

            EditorGUILayout.Space(8);

            // Corridor params
            EditorGUILayout.LabelField("Параметры коридора", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(_sectionStyle);
            {
                _connectCorridorData.width = Mathf.Max(1.5f,
                    EditorGUILayout.FloatField("Ширина", _connectCorridorData.width));
                _connectCorridorData.height = Mathf.Max(2.5f,
                    EditorGUILayout.FloatField("Высота", _connectCorridorData.height));
                _connectCorridorData.length = Mathf.Max(1f,
                    EditorGUILayout.FloatField("Длина", _connectCorridorData.length));
                _connectCorridorData.wallThickness = EditorGUILayout.Slider(
                    "Толщина стен", _connectCorridorData.wallThickness, 0.05f, 0.5f);

                EditorGUILayout.Space(4);

                _connectCorridorData.floorMaterial = (Material)EditorGUILayout.ObjectField(
                    "Пол", _connectCorridorData.floorMaterial, typeof(Material), false);
                _connectCorridorData.ceilingMaterial = (Material)EditorGUILayout.ObjectField(
                    "Потолок", _connectCorridorData.ceilingMaterial, typeof(Material), false);
                _connectCorridorData.wallMaterial = (Material)EditorGUILayout.ObjectField(
                    "Стены", _connectCorridorData.wallMaterial, typeof(Material), false);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(8);

            // Check if door is already connected
            bool alreadyConnected = false;
            foreach (var conn in _sourceRoom.Connections)
            {
                if (conn.sourceDoorIndex == _doorIndex && conn.connectedCorridor != null)
                {
                    alreadyConnected = true;
                    break;
                }
            }

            if (alreadyConnected)
            {
                EditorGUILayout.HelpBox(
                    "К этой двери уже подключён коридор!", MessageType.Warning);
            }

            // Connect button
            GUI.backgroundColor = new Color(0.95f, 0.65f, 0.2f);
            if (GUILayout.Button("\U0001F517 Соединить коридор с дверью",
                GUILayout.Height(38)))
            {
                LabGeometryBuilder.ConnectCorridorToRoom(
                    _sourceRoom, _doorIndex, _connectCorridorData);
            }
            GUI.backgroundColor = Color.white;
        }
    }
}
#endif