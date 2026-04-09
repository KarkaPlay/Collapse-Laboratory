using CollapseSettings;
using UnityEditor;
using UnityEngine;

namespace CLEditor
{
    /// <summary>
    /// Визард для быстрого создания схлопываемых объектов.
    /// Tools → Collapse Lab → ...
    /// </summary>
    public class CollapsibleCreatorWizard : EditorWindow
    {
        private string _objectName = "NewCollapsible";
        private StabilityLevel _stabilityLevel = StabilityLevel.Weak;
        private CollapseState _initialState = CollapseState.Old;
        private GameObject _oldPrefab;
        private GameObject _newPrefab;
        private bool _addLinkController;
        private Transform _parent;

        // Материалы
        private Material _oldMaterial;
        private Material _newMaterial;
        private bool _useCustomMaterials;

        // Настройки
        private CollapseLabSettings _settings;

        [MenuItem("Tools/Collapse Lab/Создать Collapsible объект", false, 1)]
        public static void ShowWindow()
        {
            var window = GetWindow<CollapsibleCreatorWizard>("Создание Collapsible");
            window.minSize = new Vector2(450, 550);
            window.LoadSettings();
        }

        [MenuItem("Tools/Collapse Lab/Создать из выделенных (Old + New)", false, 2)]
        public static void CreateFromSelection()
        {
            var selected = Selection.gameObjects;
            if (selected.Length != 2)
            {
                EditorUtility.DisplayDialog("Ошибка",
                    "Выделите ровно 2 объекта:\n1) Old-версию\n2) New-версию", "OK");
                return;
            }

            CreateCollapsibleFromExisting(selected[0], selected[1]);
        }

        [MenuItem("Tools/Collapse Lab/Создать PuzzleController", false, 20)]
        public static void CreatePuzzleController()
        {
            var go = new GameObject("PuzzleController");
            go.AddComponent<PuzzleController>();
            Selection.activeGameObject = go;
            Undo.RegisterCreatedObjectUndo(go, "Create PuzzleController");
            Debug.Log("PuzzleController создан! Настройте условия в инспекторе.");
        }

        [MenuItem("Tools/Collapse Lab/Открыть настройки", false, 100)]
        public static void OpenSettings()
        {
            var settings = Resources.Load<CollapseLabSettings>("CollapseLabSettings");
            if (settings != null)
            {
                Selection.activeObject = settings;
                EditorGUIUtility.PingObject(settings);
            }
            else
            {
                if (EditorUtility.DisplayDialog("Настройки не найдены",
                        "CollapseLabSettings не найден в папке Resources.\n\nСоздать?",
                        "Создать", "Отмена"))
                {
                    CreateSettings();
                }
            }
        }

        [MenuItem("Tools/Collapse Lab/Создать настройки (CollapseLabSettings)", false, 101)]
        public static void CreateSettings()
        {
            // Создаём папку Resources если её нет
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            var settings = ScriptableObject.CreateInstance<CollapseLabSettings>();
            AssetDatabase.CreateAsset(settings, "Assets/Resources/CollapseLabSettings.asset");
            AssetDatabase.SaveAssets();

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);

            Debug.Log("CollapseLabSettings создан в Assets/Resources/CollapseLabSettings.asset");
        }

        private void LoadSettings()
        {
            _settings = CollapseLabSettings.Instance;
            if (_settings != null)
            {
                _oldMaterial = _settings.defaultOldMaterial;
                _newMaterial = _settings.defaultNewMaterial;
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
            EditorGUILayout.LabelField("🔧 Создание Collapsible объекта", titleStyle);

            EditorGUILayout.Space(15);

            // === Проверка настроек ===
            if (_settings == null)
            {
                EditorGUILayout.HelpBox(
                    "CollapseLabSettings не найден!\n" +
                    "Создайте через: Tools → Collapse Lab → Создать настройки\n" +
                    "Или: Assets → Create → Collapse Lab → Settings",
                    MessageType.Warning);

                if (GUILayout.Button("Создать настройки", GUILayout.Height(30)))
                {
                    CreateSettings();
                    LoadSettings();
                }

                EditorGUILayout.Space(10);
            }

            // === Основные параметры ===
            EditorGUILayout.LabelField("Основные параметры", EditorStyles.boldLabel);

            _objectName = EditorGUILayout.TextField("Имя объекта", _objectName);
            _stabilityLevel = (StabilityLevel)EditorGUILayout.EnumPopup("Стабильность", _stabilityLevel);
            _initialState = (CollapseState)EditorGUILayout.EnumPopup("Начальное состояние", _initialState);

            EditorGUILayout.Space(10);

            // === Модели ===
            EditorGUILayout.LabelField("Модели (опционально)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Если указать префабы/объекты, они будут использованы как Old и New версии.\n" +
                "Если оставить пустым — будут созданы кубы-заглушки.",
                MessageType.Info);

            _oldPrefab = (GameObject)EditorGUILayout.ObjectField("Old (прошлое)", _oldPrefab, typeof(GameObject), true);
            _newPrefab =
                (GameObject)EditorGUILayout.ObjectField("New (настоящее)", _newPrefab, typeof(GameObject), true);

            EditorGUILayout.Space(10);

            // === Материалы ===
            EditorGUILayout.LabelField("Материалы", EditorStyles.boldLabel);

            _useCustomMaterials = EditorGUILayout.Toggle("Использовать свои материалы", _useCustomMaterials);

            if (_useCustomMaterials)
            {
                EditorGUI.indentLevel++;
                _oldMaterial =
                    (Material)EditorGUILayout.ObjectField("Материал OLD", _oldMaterial, typeof(Material), false);
                _newMaterial =
                    (Material)EditorGUILayout.ObjectField("Материал NEW", _newMaterial, typeof(Material), false);
                EditorGUI.indentLevel--;

                // Предупреждение о шейдере
                if (_oldMaterial != null && !_oldMaterial.shader.name.Contains("TemporalInstability") &&
                    !_oldMaterial.shader.name.Contains("Dissolve"))
                {
                    EditorGUILayout.HelpBox(
                        "Материал OLD не использует шейдер с Dissolve!\n" +
                        "Рекомендуется: Custom/TemporalInstability",
                        MessageType.Warning);
                }
            }
            else
            {
                if (_settings != null)
                {
                    EditorGUILayout.HelpBox(
                        $"Old: {(_settings.defaultOldMaterial != null ? _settings.defaultOldMaterial.name : "не назначен")}\n" +
                        $"New: {(_settings.defaultNewMaterial != null ? _settings.defaultNewMaterial.name : "не назначен")}",
                        MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("Будут использованы материалы по умолчанию (URP/Lit)", MessageType.Info);
                }
            }

            EditorGUILayout.Space(10);

            // === Дополнительно ===
            EditorGUILayout.LabelField("Дополнительно", EditorStyles.boldLabel);
            _addLinkController = EditorGUILayout.Toggle("Добавить CollapseLinkController", _addLinkController);
            _parent = (Transform)EditorGUILayout.ObjectField("Родитель", _parent, typeof(Transform), true);

            EditorGUILayout.Space(20);

            // === Кнопка создания ===
            GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
            if (GUILayout.Button("✓ СОЗДАТЬ ОБЪЕКТ", GUILayout.Height(40)))
            {
                CreateCollapsible();
            }

            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(10);

            // === Превью структуры ===
            EditorGUILayout.LabelField("Будет создана структура:", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"📁 {_objectName}");
            EditorGUILayout.LabelField($"  ├── Collapsible ({_stabilityLevel})");
            if (_addLinkController)
            {
                EditorGUILayout.LabelField($"  ├── CollapseLinkController");
                EditorGUILayout.LabelField($"  ├── TrailMoving");
            }

            EditorGUILayout.LabelField($"  ├── 📁 {_objectName}_OLD");
            EditorGUILayout.LabelField($"  │   ├── COState + Outline + Dissolvable");
            EditorGUILayout.LabelField(
                $"  │   └── {(_oldPrefab != null ? _oldPrefab.name : "Cube")} + Collider + COStateChild");
            EditorGUILayout.LabelField($"  └── 📁 {_objectName}_NEW");
            EditorGUILayout.LabelField($"      ├── COState + Outline + Dissolvable");
            EditorGUILayout.LabelField(
                $"      └── {(_newPrefab != null ? _newPrefab.name : "Cube")} + Collider + COStateChild");
            EditorGUILayout.EndVertical();
        }

        private void CreateCollapsible()
        {
            // Root
            var root = new GameObject(_objectName);
            if (_parent != null) root.transform.SetParent(_parent);
            Undo.RegisterCreatedObjectUndo(root, $"Create Collapsible {_objectName}");

            var collapsible = root.AddComponent<Collapsible>();
            collapsible.stabilityLevel = _stabilityLevel;
            collapsible.initialState = _initialState;

            if (_addLinkController)
            {
                var trailMoving = root.AddComponent<TrailMoving>();
                if (_settings != null && _settings.trailPrefab != null)
                {
                    trailMoving.trailPrefab = _settings.trailPrefab;
                }

                root.AddComponent<CollapseLinkController>();
            }

            // Определяем материалы
            Material oldMat = _useCustomMaterials ? _oldMaterial : (_settings?.defaultOldMaterial);
            Material newMat = _useCustomMaterials ? _newMaterial : (_settings?.defaultNewMaterial);

            // Old state
            var oldGo = CreateStateObject(root.transform, $"{_objectName}_OLD", _oldPrefab, oldMat, CollapseState.Old);
            var oldCOState = oldGo.GetComponent<COState>();
            oldCOState.parentCollapsible = collapsible;

            // New state
            var newGo = CreateStateObject(root.transform, $"{_objectName}_NEW", _newPrefab, newMat, CollapseState.New);
            var newCOState = newGo.GetComponent<COState>();
            newCOState.parentCollapsible = collapsible;

            // Assign references
            collapsible.stateOld = oldCOState;
            collapsible.stateNew = newCOState;

            // Select
            Selection.activeGameObject = root;
            EditorUtility.SetDirty(root);

            Debug.Log($"✓ Collapsible '{_objectName}' создан успешно!");
        }

        private GameObject CreateStateObject(Transform parent, string name, GameObject prefab, Material material,
            CollapseState state)
        {
            var stateGo = new GameObject(name);
            stateGo.transform.SetParent(parent);
            stateGo.transform.localPosition = Vector3.zero;

            // Добавляем COState на родительский объект состояния
            var coState = stateGo.AddComponent<COState>();

            // Outline на COState объекте
            var outline = stateGo.GetComponent<Outline>();
            if (outline == null)
                outline = stateGo.AddComponent<Outline>();
            outline.enabled = false;
            outline.OutlineWidth = _settings != null ? _settings.defaultOutlineWidth : 3f;

            // Dissolvable на COState объекте
            var dissolvable = stateGo.GetComponent<Dissolvable>();
            if (dissolvable == null)
                dissolvable = stateGo.AddComponent<Dissolvable>();
            dissolvable.timeToDissolve = _settings != null ? _settings.defaultDissolveTime : 0.5f;

            // Создаём визуальный объект (куб или префаб)
            GameObject visualObject;

            if (prefab != null)
            {
                visualObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, stateGo.transform);
                if (visualObject == null)
                {
                    visualObject = Instantiate(prefab, stateGo.transform);
                    visualObject.name = prefab.name;
                }

                visualObject.transform.localPosition = Vector3.zero;
            }
            else
            {
                // Заглушка — куб
                visualObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visualObject.name = "Visual";
                visualObject.transform.SetParent(stateGo.transform);
                visualObject.transform.localPosition = Vector3.zero;

                // Применяем материал
                var renderer = visualObject.GetComponent<Renderer>();
                if (material != null)
                {
                    renderer.sharedMaterial = material;
                }
                else if (_settings != null)
                {
                    renderer.sharedMaterial = _settings.CreateMaterialForState(state);
                }
                else
                {
                    // Fallback
                    var fallbackShader = Shader.Find("Custom/TemporalInstability");
                    if (fallbackShader == null)
                        fallbackShader = Shader.Find("Universal Render Pipeline/Lit");

                    var mat = new Material(fallbackShader);
                    mat.color = state == CollapseState.Old
                        ? new Color(0.8f, 0.7f, 0.5f)
                        : new Color(0.4f, 0.5f, 0.6f);
                    renderer.sharedMaterial = mat;
                }
            }

            // === ВАЖНО: Добавляем COStateChild на визуальный объект ===
            // Это позволяет raycast попадать в визуальный объект и корректно обрабатывать взаимодействие
            SetupVisualChildren(stateGo.transform, coState);

            // Настраиваем Dissolvable
            dissolvable.SetRenderersInChildren();
            dissolvable.SetCollidersInChildren();

            // Связываем COState
            coState.SetParentOutlineAndDissolve();

            return stateGo;
        }

        /// <summary>
        /// Добавляет COStateChild и Collider ко всем дочерним объектам с рендерерами.
        /// </summary>
        private static void SetupVisualChildren(Transform parent, COState parentCOState)
        {
            var renderers = parent.GetComponentsInChildren<Renderer>();

            foreach (var renderer in renderers)
            {
                var go = renderer.gameObject;

                // Добавляем коллайдер если нет
                if (go.GetComponent<Collider>() == null)
                {
                    // Пробуем MeshCollider для MeshRenderer
                    var meshFilter = go.GetComponent<MeshFilter>();
                    if (meshFilter != null && meshFilter.sharedMesh != null)
                    {
                        var meshCollider = go.AddComponent<MeshCollider>();
                        meshCollider.convex = true; // Для взаимодействия
                    }
                    else
                    {
                        // Fallback на BoxCollider
                        go.AddComponent<BoxCollider>();
                    }
                }

                // Добавляем COStateChild
                var coStateChild = go.GetComponent<COStateChild>();
                if (coStateChild == null)
                {
                    coStateChild = go.AddComponent<COStateChild>();
                }

                coStateChild.parentCOState = parentCOState;

                // Устанавливаем layer для raycast (если не установлен)
                // Предполагаем что есть layer "Interactable"
                int interactableLayer = LayerMask.NameToLayer("Interactable");
                if (interactableLayer >= 0)
                {
                    go.layer = interactableLayer;
                }
            }
        }

        private static void CreateCollapsibleFromExisting(GameObject oldObj, GameObject newObj)
        {
            string baseName = oldObj.name.Replace("_OLD", "").Replace("_old", "")
                .Replace("_Old", "").Replace("Old", "").Trim();
            if (string.IsNullOrEmpty(baseName))
                baseName = "Collapsible";

            var root = new GameObject(baseName);
            root.transform.position = (oldObj.transform.position + newObj.transform.position) / 2f;
            Undo.RegisterCreatedObjectUndo(root, $"Create Collapsible from selection");

            var collapsible = root.AddComponent<Collapsible>();

            // Переносим объекты под root
            Undo.SetTransformParent(oldObj.transform, root.transform, "Reparent OLD");
            Undo.SetTransformParent(newObj.transform, root.transform, "Reparent NEW");

            oldObj.name = $"{baseName}_OLD";
            newObj.name = $"{baseName}_NEW";

            // Добавляем компоненты
            var oldCOState = oldObj.GetComponent<COState>();
            if (oldCOState == null) oldCOState = oldObj.AddComponent<COState>();

            var newCOState = newObj.GetComponent<COState>();
            if (newCOState == null) newCOState = newObj.AddComponent<COState>();

            // Setup COState
            SetupExistingCOState(oldCOState, collapsible);
            SetupExistingCOState(newCOState, collapsible);

            collapsible.stateOld = oldCOState;
            collapsible.stateNew = newCOState;

            Selection.activeGameObject = root;
            EditorUtility.SetDirty(root);

            Debug.Log($"✓ Collapsible '{baseName}' создан из выделенных объектов!");
        }

        private static void SetupExistingCOState(COState coState, Collapsible parent)
        {
            coState.parentCollapsible = parent;

            // Outline
            var outline = coState.GetComponent<Outline>();
            if (outline == null)
                outline = coState.gameObject.AddComponent<Outline>();
            outline.enabled = false;

            // Dissolvable
            var dissolvable = coState.GetComponent<Dissolvable>();
            if (dissolvable == null)
                dissolvable = coState.gameObject.AddComponent<Dissolvable>();
            dissolvable.SetRenderersInChildren();
            dissolvable.SetCollidersInChildren();

            // Связываем
            coState.SetParentOutlineAndDissolve();

            // Добавляем COStateChild к дочерним объектам
            SetupVisualChildren(coState.transform, coState);

            // Layer
            int interactableLayer = LayerMask.NameToLayer("Interactable");
            if (interactableLayer >= 0)
            {
                SetLayerRecursively(coState.gameObject, interactableLayer);
            }
        }

        private static void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }
    }
}