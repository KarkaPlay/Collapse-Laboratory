using UnityEngine;

namespace CollapseSettings
{
    /// <summary>
    /// Глобальные настройки Collapse Laboratory.
    /// Создайте через Assets → Create → Collapse Lab → Settings
    /// </summary>
    [CreateAssetMenu(fileName = "CollapseLabSettings", menuName = "Collapse Lab/Settings", order = 1)]
    public class CollapseLabSettings : ScriptableObject
    {
        private static CollapseLabSettings _instance;

        public static CollapseLabSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<CollapseLabSettings>("CollapseLabSettings");
                    if (_instance == null)
                    {
                        Debug.LogWarning(
                            "[CollapseLabSettings] Настройки не найдены в Resources/CollapseLabSettings. " +
                            "Создайте через Assets → Create → Collapse Lab → Settings и поместите в папку Resources.");
                    }
                }

                return _instance;
            }
        }

        [Header("Материалы по умолчанию")]
        [Tooltip("Материал для OLD-состояния (прошлое, 1980-е). Должен использовать шейдер Custom/TemporalInstability")]
        public Material defaultOldMaterial;

        [Tooltip("Материал для NEW-состояния (настоящее, разрушенное). Должен использовать шейдер Custom/TemporalInstability")]
        public Material defaultNewMaterial;

        [Header("Шейдер")]
        [Tooltip("Шейдер для Dissolve эффекта")]
        public Shader temporalInstabilityShader;

        [Header("Outline")]
        [Tooltip("Толщина обводки по умолчанию")]
        [Range(1f, 10f)]
        public float defaultOutlineWidth = 3f;

        [Header("Цвета Outline по стабильности")]
        public Color absoluteOutlineColor = new Color(0.5f, 0.5f, 0.5f);
        public Color strongOutlineColor = new Color(1f, 0.8f, 0f);
        public Color weakOutlineColor = new Color(0.3f, 0.7f, 1f);
        public Color unstableOutlineColor = new Color(1f, 0.3f, 0.3f);

        [Header("Dissolve")]
        [Tooltip("Время анимации dissolve по умолчанию")]
        [Range(0.1f, 3f)]
        public float defaultDissolveTime = 0.5f;

        [Header("Trail эффект")]
        [Tooltip("Префаб для визуализации связей (trail)")]
        public GameObject trailPrefab;

        /// <summary>
        /// Получить цвет outline для уровня стабильности.
        /// </summary>
        public Color GetOutlineColor(StabilityLevel level)
        {
            return level switch
            {
                StabilityLevel.Absolute => absoluteOutlineColor,
                StabilityLevel.Strong => strongOutlineColor,
                StabilityLevel.Weak => weakOutlineColor,
                StabilityLevel.Unstable => unstableOutlineColor,
                _ => Color.white
            };
        }

        /// <summary>
        /// Создать материал для состояния с нужным шейдером.
        /// </summary>
        public Material CreateMaterialForState(CollapseState state)
        {
            Material baseMaterial = state == CollapseState.Old ? defaultOldMaterial : defaultNewMaterial;

            if (baseMaterial != null)
            {
                return new Material(baseMaterial);
            }

            // Fallback если материал не назначен
            if (temporalInstabilityShader != null)
            {
                var mat = new Material(temporalInstabilityShader);
                mat.color = state == CollapseState.Old
                    ? new Color(0.8f, 0.7f, 0.5f)
                    : new Color(0.4f, 0.5f, 0.6f);
                return mat;
            }

            // Совсем fallback
            var fallbackShader = Shader.Find("Custom/TemporalInstability");
            if (fallbackShader == null)
            {
                fallbackShader = Shader.Find("Universal Render Pipeline/Lit");
            }

            var fallbackMat = new Material(fallbackShader);
            fallbackMat.color = state == CollapseState.Old
                ? new Color(0.8f, 0.7f, 0.5f)
                : new Color(0.4f, 0.5f, 0.6f);
            return fallbackMat;
        }
    }
}