using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LabBuilder.Data
{
    /// <summary>
    /// Глобальные настройки Lab Builder.
    /// Хранит материалы по умолчанию и другие параметры.
    /// </summary>
    [CreateAssetMenu(fileName = "LabBuilderSettings", menuName = "Lab Builder/Settings")]
    public sealed class LabBuilderSettings : ScriptableObject
    {
        private static LabBuilderSettings _instance;

        [Header("Материалы по умолчанию")]
        [SerializeField] private Material _defaultFloorMaterial;
        [SerializeField] private Material _defaultCeilingMaterial;
        [SerializeField] private Material _defaultWallMaterial;

        [Header("Размеры по умолчанию")]
        [SerializeField, Min(2f)] private float _defaultRoomWidth = 5f;
        [SerializeField, Min(2f)] private float _defaultRoomLength = 5f;
        [SerializeField, Min(2.5f)] private float _defaultRoomHeight = 3f;
        [SerializeField, Range(0.05f, 0.5f)] private float _defaultWallThickness = 0.15f;

        [Header("Настройки дверей")]
        [SerializeField, Min(0.7f)] private float _defaultDoorWidth = 1.2f;
        [SerializeField, Min(1.5f)] private float _defaultDoorHeight = 2.4f;

        [Header("Настройки соединений")]
        [SerializeField, Min(0.5f)] private float _minConnectionLength = 1f;
        [SerializeField, Min(0.5f)] private float _defaultConnectionLength = 3f;
        [SerializeField, Min(1f)] private float _maxConnectionLength = 20f;

        [Header("Визуализация")]
        [SerializeField] private Color _doorGizmoColor = new Color(0.2f, 0.9f, 0.3f, 0.8f);
        [SerializeField] private Color _connectionPreviewColor = new Color(0.9f, 0.6f, 0.1f, 0.5f);
        [SerializeField] private bool _showDoorLabels = true;

        // Properties
        public Material DefaultFloorMaterial => _defaultFloorMaterial;
        public Material DefaultCeilingMaterial => _defaultCeilingMaterial;
        public Material DefaultWallMaterial => _defaultWallMaterial;

        public float DefaultRoomWidth => _defaultRoomWidth;
        public float DefaultRoomLength => _defaultRoomLength;
        public float DefaultRoomHeight => _defaultRoomHeight;
        public float DefaultWallThickness => _defaultWallThickness;

        public float DefaultDoorWidth => _defaultDoorWidth;
        public float DefaultDoorHeight => _defaultDoorHeight;

        public float MinConnectionLength => _minConnectionLength;
        public float DefaultConnectionLength => _defaultConnectionLength;
        public float MaxConnectionLength => _maxConnectionLength;

        public Color DoorGizmoColor => _doorGizmoColor;
        public Color ConnectionPreviewColor => _connectionPreviewColor;
        public bool ShowDoorLabels => _showDoorLabels;

        /// <summary>Singleton доступ к настройкам.</summary>
        public static LabBuilderSettings Instance
        {
            get
            {
                if (_instance == null)
                {
#if UNITY_EDITOR
                    _instance = LoadOrCreateSettings();
#else
                    _instance = Resources.Load<LabBuilderSettings>("LabBuilderSettings");
#endif
                }
                return _instance;
            }
        }

#if UNITY_EDITOR
        private static LabBuilderSettings LoadOrCreateSettings()
        {
            // Ищем существующий файл
            var guids = AssetDatabase.FindAssets("t:LabBuilderSettings");

            if (guids.Length > 0)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<LabBuilderSettings>(assetPath);
            }

            // Создаём новый
            var settings = CreateInstance<LabBuilderSettings>();

            const string folder = "Assets/LabBuilder/Resources";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets/LabBuilder", "Resources");
            }

            const string path = folder + "/LabBuilderSettings.asset";
            AssetDatabase.CreateAsset(settings, path);
            AssetDatabase.SaveAssets();

            Debug.Log($"[LabBuilder] Created settings at {path}");
            return settings;
        }

        [MenuItem("Tools/Lab Builder Settings")]
        private static void SelectSettings()
        {
            Selection.activeObject = Instance;
            EditorGUIUtility.PingObject(Instance);
        }
#endif
    }
}