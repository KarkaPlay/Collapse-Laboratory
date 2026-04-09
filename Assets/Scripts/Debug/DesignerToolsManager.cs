using CollapseSettings;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Интерактивный редактор объектов в игре.
/// F1 - открыть/закрыть
/// Клик на метку объекта - открыть редактор объекта
/// </summary>
public class DesignerToolsManager : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Клавиша для включения/выключения")]
    public Key toggleKey = Key.F1;

    [Tooltip("Клавиша для следующей страницы")]
    public Key nextPageKey = Key.F2;

    [Tooltip("Клавиша для предыдущей страницы")]
    public Key prevPageKey = Key.F3;

    [Header("Ссылки на игрока")]
    [Tooltip("Компонент управления игроком")]
    public MonoBehaviour playerController;

    [Tooltip("Компонент ввода игрока")]
    public MonoBehaviour playerInput;

    [Tooltip("Компонент взаимодействия игрока")]
    public PlayerInteraction playerInteraction;

    [Header("Визуализация")]
    [Tooltip("Показывать метки над объектами")]
    public bool showWorldLabels = true;

    [Tooltip("Максимальная дистанция меток")]
    [Range(5f, 100f)]
    public float maxLabelDistance = 30f;

    [Tooltip("Размер кликабельной зоны метки")]
    [Range(10f, 100f)]
    public float labelClickRadius = 40f;

    [Header("Состояние")]
    [SerializeField] private bool _isActive = false;

    public bool IsActive => _isActive;

    // Сохранённое состояние курсора
    private CursorLockMode _previousLockMode;
    private bool _previousCursorVisible;

    // Кэш объектов
    private Collapsible[] _allCollapsibles;
    private PuzzleController[] _allPuzzles;
    private CollapseLinkController[] _allLinks;
    private CollapsibleGroupController[] _allGroups;

    // Для выбора цели связи
    private bool _isSelectingLinkTarget = false;
    private int _selectingLinkIndex = -1;

    private int _currentPage = 0;
    private Vector2 _scrollPosition;

    // UI стили
    private GUIStyle _boxStyle;
    private GUIStyle _headerStyle;
    private GUIStyle _labelStyle;
    private GUIStyle _statusStyle;
    private GUIStyle _buttonStyle;
    private GUIStyle _smallButtonStyle;
    private GUIStyle _toggleStyle;
    private bool _stylesInitialized = false;

    // Редактируемый объект
    private Collapsible _editingCollapsible;
    private CollapseLinkController _editingLinkController;
    private CollapsibleGroupController _editingGroup;

    // Для кликов по меткам
    private Dictionary<Collapsible, Rect> _labelRects = new();

    private Camera _playerCamera;

    private void Awake()
    {
        if (playerController == null || playerInput == null)
        {
            TryFindPlayerComponents();
        }

        _playerCamera = Camera.main;
    }

    private void TryFindPlayerComponents()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            player = GameObject.Find("Player");
        }

        if (player != null)
        {
            if (playerController == null)
            {
                playerController = player.GetComponent("FirstPersonController") as MonoBehaviour;
            }

            if (playerInput == null)
            {
                playerInput = player.GetComponent("StarterAssetsInputs") as MonoBehaviour;
            }

            if (playerInteraction == null)
            {
                playerInteraction = player.GetComponent<PlayerInteraction>();
            }
        }

        if (_playerCamera == null)
        {
            _playerCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current[toggleKey].wasPressedThisFrame)
        {
            Toggle();
        }

        if (_isActive)
        {
            if (Keyboard.current[nextPageKey].wasPressedThisFrame)
            {
                _currentPage = Mathf.Min(3, _currentPage + 1);
            }

            if (Keyboard.current[prevPageKey].wasPressedThisFrame)
            {
                _currentPage = Mathf.Max(0, _currentPage - 1);
            }

            if (Keyboard.current[Key.Escape].wasPressedThisFrame)
            {
                // Если выбираем цель связи — закрыть выбор
                if (_isSelectingLinkTarget)
                {
                    _isSelectingLinkTarget = false;
                    _selectingLinkIndex = -1;
                }
                // Если редактируем объект — закрыть редактор
                else if (_editingCollapsible != null || _editingLinkController != null || _editingGroup != null)
                {
                    CloseEditor();
                }
                // Иначе закрыть Designer Tools
                else
                {
                    Deactivate();
                }
            }
        }
    }

    public void Toggle()
    {
        if (_isActive)
        {
            Deactivate();
        }
        else
        {
            Activate();
        }
    }

    private void Activate()
    {
        _isActive = true;

        _previousLockMode = Cursor.lockState;
        _previousCursorVisible = Cursor.visible;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetPlayerControlsEnabled(false);
        RefreshCache();

        Debug.Log("[DesignerTools] Активированы. ESC для выхода, клик на метку для редактирования.");
    }

    private void Deactivate()
    {
        _isActive = false;

        Cursor.lockState = _previousLockMode;
        Cursor.visible = _previousCursorVisible;

        SetPlayerControlsEnabled(true);
        CloseEditor();

        Debug.Log("[DesignerTools] Деактивированы.");
    }

    private void SetPlayerControlsEnabled(bool enabled)
    {
        if (playerController != null)
        {
            playerController.enabled = enabled;
        }

        if (playerInput != null)
        {
            playerInput.enabled = enabled;
        }

        if (playerInteraction != null)
        {
            playerInteraction.enabled = enabled;
        }
    }

    private void RefreshCache()
    {
        _allCollapsibles = FindObjectsByType<Collapsible>(FindObjectsSortMode.None);
        _allPuzzles = FindObjectsByType<PuzzleController>(FindObjectsSortMode.None);
        _allLinks = FindObjectsByType<CollapseLinkController>(FindObjectsSortMode.None);
        _allGroups = FindObjectsByType<CollapsibleGroupController>(FindObjectsSortMode.None);
    }

    private void CloseEditor()
    {
        _editingCollapsible = null;
        _editingLinkController = null;
        _editingGroup = null;
        _isSelectingLinkTarget = false;
        _selectingLinkIndex = -1;
    }

    private void InitStyles()
    {
        if (_stylesInitialized) return;

        // Масштабирование под разрешение
        float scaleFactor = Screen.height / 1080f; // Базовое разрешение 1920x1080
        scaleFactor = Mathf.Clamp(scaleFactor, 0.8f, 2f); // Ограничиваем от 0.8x до 2x

        int baseFontSize = Mathf.RoundToInt(14 * scaleFactor);
        int headerFontSize = Mathf.RoundToInt(20 * scaleFactor);
        int smallFontSize = Mathf.RoundToInt(12 * scaleFactor);
        int padding = Mathf.RoundToInt(10 * scaleFactor);

        // === Box Style ===
        _boxStyle = new GUIStyle(GUI.skin.box)
        {
            padding = new RectOffset(padding, padding, padding, padding),
            margin = new RectOffset(0, 0, 4, 4)
        };

        // === Header Style ===
        _headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = headerFontSize,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.95f, 0.7f) }, // Тёплый белый
            padding = new RectOffset(0, 0, 4, 4)
        };

        // === Label Style ===
        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = baseFontSize,
            normal = { textColor = new Color(0.9f, 0.9f, 0.9f) },
            wordWrap = true,
            padding = new RectOffset(0, 0, 2, 2)
        };

        // === Status Style ===
        _statusStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = baseFontSize,
            wordWrap = true,
            normal = { textColor = new Color(0.85f, 0.85f, 0.85f) },
            padding = new RectOffset(0, 0, 2, 2)
        };

        // === Button Style ===
        _buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = baseFontSize,
            fontStyle = FontStyle.Bold,
            padding = new RectOffset(padding, padding, padding / 2, padding / 2),
            margin = new RectOffset(2, 2, 2, 2),
            normal =
        {
            textColor = Color.white,
            background = MakeTex(2, 2, new Color(0.25f, 0.35f, 0.5f, 0.9f))
        },
            hover =
        {
            textColor = Color.white,
            background = MakeTex(2, 2, new Color(0.35f, 0.5f, 0.7f, 0.9f))
        },
            active =
        {
            textColor = new Color(1f, 1f, 0.8f),
            background = MakeTex(2, 2, new Color(0.2f, 0.3f, 0.45f, 0.9f))
        }
        };

        // === Small Button Style ===
        _smallButtonStyle = new GUIStyle(_buttonStyle)
        {
            fontSize = smallFontSize,
            padding = new RectOffset(padding / 2, padding / 2, padding / 3, padding / 3)
        };

        // === Toggle Style ===
        _toggleStyle = new GUIStyle(GUI.skin.toggle)
        {
            fontSize = baseFontSize,
            normal = { textColor = new Color(0.9f, 0.9f, 0.9f) },
            padding = new RectOffset(padding + 5, 0, 2, 2)
        };

        _stylesInitialized = true;
    }

    // Вспомогательный метод для создания цветных текстур
    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
        {
            pix[i] = col;
        }

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private void OnGUI()
    {
        if (!_isActive) return;

        InitStyles();

        // Обрабатываем клики по меткам
        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            HandleLabelClicks();
        }

        // Основная панель
        DrawMainPanel();

        // Мировые метки
        if (showWorldLabels)
        {
            DrawWorldLabels();
        }
    }

    #region Main Panel

    private void DrawMainPanel()
    {
        // Адаптивные размеры панели
        float scaleFactor = Screen.height / 1080f;
        scaleFactor = Mathf.Clamp(scaleFactor, 0.8f, 2f);

        float panelWidth = 550 * scaleFactor;
        float panelHeight = Screen.height - (40 * scaleFactor);
        float margin = 10 * scaleFactor;

        // Фон с тенью
        GUI.color = new Color(0, 0, 0, 0.3f);
        GUI.DrawTexture(new Rect(margin + 4, margin + 4, panelWidth, panelHeight), Texture2D.whiteTexture);

        // Основной фон
        GUI.color = new Color(0.08f, 0.08f, 0.12f, 0.95f); // Тёмно-синий
        GUI.DrawTexture(new Rect(margin, margin, panelWidth, panelHeight), Texture2D.whiteTexture);

        // Рамка
        GUI.color = new Color(0.3f, 0.5f, 0.7f, 0.8f); // Голубая рамка
        DrawBox(new Rect(margin, margin, panelWidth, panelHeight), 2 * scaleFactor);

        GUI.color = Color.white;

        GUILayout.BeginArea(new Rect(margin + 15, margin + 15, panelWidth - 30, panelHeight - 30));

        // Заголовок с градиентным фоном
        Rect headerRect = GUILayoutUtility.GetRect(0, 40 * scaleFactor, GUILayout.ExpandWidth(true));
        GUI.color = new Color(0.2f, 0.35f, 0.55f, 0.7f);
        GUI.DrawTexture(headerRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUILayout.BeginArea(headerRect);
        GUILayout.BeginHorizontal();
        GUILayout.Space(10);

        GUIStyle titleStyle = new GUIStyle(_headerStyle)
        {
            fontSize = Mathf.RoundToInt(24 * scaleFactor)
        };
        GUILayout.Label("🔧 DESIGNER TOOLS", titleStyle);
        GUILayout.FlexibleSpace();

        GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f);
        if (GUILayout.Button("✕", _buttonStyle, GUILayout.Width(40 * scaleFactor), GUILayout.Height(35 * scaleFactor)))
        {
            Deactivate();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(5);
        GUILayout.EndHorizontal();
        GUILayout.EndArea();

        GUILayout.Label(
            $"[{toggleKey}] Вкл/Выкл  |  Клик на метку — редактирование  |  [ESC] Назад/Закрыть",
            _labelStyle);
        GUILayout.Space(5);

        // Если редактируем объект — показываем редактор
        if (_editingCollapsible != null)
        {
            DrawObjectEditor();
        }
        else if (_editingGroup != null)
        {
            DrawGroupEditor();
        }
        else
        {
            // Иначе показываем навигацию и страницы
            DrawNavigation();
            DrawSeparator();

            if (GUILayout.Button("🔄 Обновить данные", _buttonStyle, GUILayout.Height(25)))
            {
                RefreshCache();
            }

            GUILayout.Space(5);

            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            switch (_currentPage)
            {
                case 0:
                    DrawOverviewPage();
                    break;
                case 1:
                    DrawObjectsListPage();
                    break;
                case 2:
                    DrawPuzzlesPage();
                    break;
                case 3:
                    DrawGroupsPage();
                    break;
            }

            GUILayout.EndScrollView();
        }

        GUILayout.EndArea();
    }

    /// <summary>
    /// Рисует рамку вокруг rect заданной толщины
    /// </summary>
    private void DrawBox(Rect rect, float thickness)
    {
        // Верх
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
        // Низ
        GUI.DrawTexture(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), Texture2D.whiteTexture);
        // Лево
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
        // Право
        GUI.DrawTexture(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
    }

    private void DrawNavigation()
    {
        float scaleFactor = Screen.height / 1080f;
        scaleFactor = Mathf.Clamp(scaleFactor, 0.8f, 2f);

        string[] pages = { "📊 Обзор", "📦 Объекты", "🧩 Головоломки", "⚡ Группы" };
        _currentPage = Mathf.Clamp(_currentPage, 0, pages.Length - 1);

        GUILayout.BeginHorizontal();
        for (int i = 0; i < pages.Length; i++)
        {
            bool isActive = i == _currentPage;

            GUI.backgroundColor = isActive
                ? new Color(0.3f, 0.6f, 1f)
                : new Color(0.2f, 0.25f, 0.35f);

            GUIStyle tabStyle = new GUIStyle(_buttonStyle)
            {
                fontSize = Mathf.RoundToInt(14 * scaleFactor),
                fontStyle = isActive ? FontStyle.Bold : FontStyle.Normal,
                normal = { textColor = isActive ? Color.white : new Color(0.7f, 0.7f, 0.7f) }
            };

            if (GUILayout.Button(pages[i], tabStyle, GUILayout.Height(35 * scaleFactor)))
            {
                _currentPage = i;
            }
        }

        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();
        GUILayout.Space(10 * scaleFactor);
    }

    #endregion

    #region Object Editor

    private void DrawObjectEditor()
    {
        if (_editingCollapsible == null)
        {
            CloseEditor();
            return;
        }

        // Заголовок редактора
        GUILayout.Label($"✏️ Редактирование: {_editingCollapsible.name}", _headerStyle);
        GUILayout.Space(5);

        if (GUILayout.Button("← Назад к списку", _buttonStyle))
        {
            CloseEditor();
            return;
        }

        GUILayout.Space(10);
        DrawSeparator();

        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

        // === Основные настройки ===
        GUILayout.Label("⚙️ Основные настройки", _headerStyle);
        GUILayout.Space(5);

        // Стабильность
        GUILayout.BeginHorizontal();
        float scaleFactor = Screen.height / 1080f;
        scaleFactor = Mathf.Clamp(scaleFactor, 0.8f, 2f);

        GUILayout.Label("Стабильность:", _labelStyle, GUILayout.Width(150 * scaleFactor));
        StabilityLevel newStability =
            DrawEnumPopup(_editingCollapsible.stabilityLevel, GUILayout.Width(180));
        if (newStability != _editingCollapsible.stabilityLevel)
        {
            _editingCollapsible.stabilityLevel = newStability;
        }

        GUILayout.EndHorizontal();

        // Описание стабильности
        string stabilityDesc = _editingCollapsible.stabilityLevel switch
        {
            StabilityLevel.Absolute => "🔒 Нельзя изменить",
            StabilityLevel.Strong => "🔗 Только через связь",
            StabilityLevel.Weak => "✋ Игрок может менять",
            StabilityLevel.Unstable => "⚡ Меняется само + игрок",
            _ => ""
        };
        GUILayout.Label($"  {stabilityDesc}", _labelStyle);

        GUILayout.Space(5);

        // Начальное состояние
        GUILayout.BeginHorizontal();
        GUILayout.Label("Начальное состояние:", _labelStyle, GUILayout.Width(150 * scaleFactor));
        CollapseState newInitial = DrawEnumPopup(_editingCollapsible.initialState, GUILayout.Width(180));
        if (newInitial != _editingCollapsible.initialState)
        {
            _editingCollapsible.initialState = newInitial;
        }

        GUILayout.EndHorizontal();

        // Текущее состояние
        GUILayout.BeginHorizontal();
        GUILayout.Label("Текущее состояние:", _labelStyle, GUILayout.Width(150 * scaleFactor));
        _statusStyle.normal.textColor = _editingCollapsible.CurrentState == CollapseState.Old
            ? new Color(0.9f, 0.8f, 0.6f)
            : new Color(0.6f, 0.8f, 1f);
        GUILayout.Label(_editingCollapsible.CurrentState.ToString(), _statusStyle, GUILayout.Width(80));
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Быстрые действия
        GUILayout.Label("Быстрые действия:", _labelStyle);
        GUILayout.BeginHorizontal();

        if (_editingCollapsible.CanBeChanged)
        {
            if (GUILayout.Button("Toggle", _smallButtonStyle))
            {
                _editingCollapsible.Collapse(CollapseOrigin.Script);
            }

            if (GUILayout.Button("→ Old", _smallButtonStyle))
            {
                _editingCollapsible.Collapse(CollapseOrigin.Script, CollapseState.Old);
            }

            if (GUILayout.Button("→ New", _smallButtonStyle))
            {
                _editingCollapsible.Collapse(CollapseOrigin.Script, CollapseState.New);
            }

            if (GUILayout.Button("Reset", _smallButtonStyle))
            {
                _editingCollapsible.Reset();
            }
        }
        else
        {
            GUILayout.Label("(нельзя изменить — Absolute)", _labelStyle);
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(15);
        DrawSeparator();

        // === Запутанность (Links) ===
        DrawLinkControllerSection();

        GUILayout.Space(15);
        DrawSeparator();

        // === Дополнительно ===
        GUILayout.Label("🗑 Опасная зона", _headerStyle);
        GUILayout.Space(5);

        GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
        if (GUILayout.Button("Удалить объект", _buttonStyle, GUILayout.Height(30)))
        {
            Destroy(_editingCollapsible.gameObject);
            RefreshCache();
            CloseEditor();
        }

        GUI.backgroundColor = Color.white;

        GUILayout.EndScrollView();
    }

    private void DrawLinkControllerSection()
    {
        GUILayout.Label("🔗 Запутанность (связи)", _headerStyle);
        GUILayout.Space(5);

        var linkCtrl = _editingCollapsible.GetComponent<CollapseLinkController>();

        if (linkCtrl == null)
        {
            GUILayout.Label("У объекта нет компонента CollapseLinkController", _labelStyle);

            if (GUILayout.Button("+ Добавить CollapseLinkController", _buttonStyle))
            {
                linkCtrl = _editingCollapsible.gameObject.AddComponent<CollapseLinkController>();
                if (_editingCollapsible.GetComponent<TrailMoving>() == null)
                {
                    _editingCollapsible.gameObject.AddComponent<TrailMoving>();
                }

                _editingLinkController = linkCtrl;
            }

            return;
        }

        _editingLinkController = linkCtrl;

        GUILayout.Label($"Связей: {linkCtrl.ActiveLinkCount}", _labelStyle);
        GUILayout.Space(5);

        // Список связей
        if (linkCtrl.links == null || linkCtrl.links.Count == 0)
        {
            GUILayout.Label("Нет связей. Добавьте ниже.", _labelStyle);
        }
        else
        {
            for (int i = 0; i < linkCtrl.links.Count; i++)
            {
                var link = linkCtrl.links[i];
                DrawLinkCard(link, i, linkCtrl);
                GUILayout.Space(3);
            }
        }

        GUILayout.Space(10);

        // Кнопка добавления связи
        if (GUILayout.Button("+ Добавить новую связь", _buttonStyle))
        {
            linkCtrl.links.Add(new CollapseLink
            {
                target = null,
                triggerWhen = CollapseTriggerCondition.OnAnyCollapse,
                action = CollapseLinkAction.Toggle,
                delay = 0.3f,
                showTrail = true
            });
        }
    }

    private void DrawLinkCard(CollapseLink link, int index, CollapseLinkController linkCtrl)
    {
        float scaleFactor = Screen.height / 1080f;
        scaleFactor = Mathf.Clamp(scaleFactor, 0.8f, 2f);

        // Цветной фон для карточки связи
        GUI.backgroundColor = new Color(0.15f, 0.2f, 0.3f, 0.8f);
        GUILayout.BeginVertical(GUI.skin.box);
        GUI.backgroundColor = Color.white;

        // Заголовок связи
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Связь #{index + 1}", _statusStyle);
        GUILayout.FlexibleSpace();

        GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
        if (GUILayout.Button("✕", _smallButtonStyle, GUILayout.Width(25)))
        {
            linkCtrl.links.RemoveAt(index);
            _isSelectingLinkTarget = false;
            _selectingLinkIndex = -1;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            return;
        }

        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        // Если выбираем цель для ЭТОЙ связи — показываем список
        if (_isSelectingLinkTarget && _selectingLinkIndex == index)
        {
            DrawTargetSelector(link, index);
        }
        else
        {
            // Обычный режим — показываем настройки связи

            // Цель
            GUILayout.BeginHorizontal();
            GUILayout.Label("Цель:", _labelStyle, GUILayout.Width(80));

            string targetName = link.target != null ? link.target.name : "(не назначена)";
            Color buttonColor = link.target != null ? Color.white : new Color(1f, 0.7f, 0.3f);
            GUI.backgroundColor = buttonColor;

            if (GUILayout.Button(targetName, _buttonStyle, GUILayout.Width(180)))
            {
                // Открываем выбор цели
                _isSelectingLinkTarget = true;
                _selectingLinkIndex = index;
            }

            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            // Условие
            GUILayout.BeginHorizontal();
            GUILayout.Label("Когда:", _labelStyle, GUILayout.Width(80));
            link.triggerWhen = DrawEnumPopup(link.triggerWhen, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            // Действие
            GUILayout.BeginHorizontal();
            GUILayout.Label("Действие:", _labelStyle, GUILayout.Width(80));
            link.action = DrawEnumPopup(link.action, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            // Задержка
            GUILayout.BeginHorizontal();
            GUILayout.Label("Задержка:", _labelStyle, GUILayout.Width(80));
            string delayStr = GUILayout.TextField(link.delay.ToString("F2"), GUILayout.Width(60));
            if (float.TryParse(delayStr, out float newDelay))
            {
                link.delay = Mathf.Max(0, newDelay);
            }

            GUILayout.Label("сек", _labelStyle);
            GUILayout.EndHorizontal();

            // Trail
            GUILayout.BeginHorizontal();
            link.showTrail = GUILayout.Toggle(link.showTrail, " Показывать trail", _toggleStyle);
            GUILayout.EndHorizontal();

            // Заметка
            GUILayout.BeginHorizontal();
            GUILayout.Label("Заметка:", _labelStyle, GUILayout.Width(80));
            link.designerNote = GUILayout.TextField(link.designerNote ?? "", GUILayout.Width(280));
            GUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();
    }

    private void DrawTargetSelector(CollapseLink link, int linkIndex)
    {
        GUILayout.Label("🎯 Выберите цель связи:", _labelStyle);
        GUILayout.Space(5);

        // Кнопка отмены
        if (GUILayout.Button("← Отмена", _smallButtonStyle))
        {
            _isSelectingLinkTarget = false;
            _selectingLinkIndex = -1;
            return;
        }

        GUILayout.Space(5);

        // Список всех Collapsible (кроме текущего)
        if (_allCollapsibles == null || _allCollapsibles.Length == 0)
        {
            GUILayout.Label("Нет доступных объектов", _labelStyle);
            return;
        }

        GUILayout.Label($"Доступно объектов: {_allCollapsibles.Length}", _labelStyle);
        GUILayout.Space(3);

        foreach (var c in _allCollapsibles)
        {
            if (c == null) continue;

            // Не показываем сам редактируемый объект
            if (c == _editingCollapsible) continue;

            // Не показываем объекты, которые нельзя использовать как цель
            if (!c.CanBeLinkedTarget) continue;

            GUILayout.BeginHorizontal();

            string icon = c.stabilityLevel switch
            {
                StabilityLevel.Absolute => "🔒",
                StabilityLevel.Strong => "🔗",
                StabilityLevel.Weak => "✋",
                StabilityLevel.Unstable => "⚡",
                _ => "?"
            };

            // Подсветка если уже выбран
            bool isSelected = link.target == c;
            GUI.backgroundColor = isSelected ? new Color(0.3f, 0.8f, 0.3f) : Color.white;

            if (GUILayout.Button($"{icon} {c.name}", _smallButtonStyle, GUILayout.Width(220)))
            {
                // Назначаем цель
                link.target = c;

                // Закрываем выбор
                _isSelectingLinkTarget = false;
                _selectingLinkIndex = -1;
            }

            GUI.backgroundColor = Color.white;

            // Показываем текущее состояние
            _statusStyle.normal.textColor = c.CurrentState == CollapseState.Old
                ? new Color(0.9f, 0.8f, 0.6f)
                : new Color(0.6f, 0.8f, 1f);
            GUILayout.Label($"[{c.CurrentState}]", _statusStyle, GUILayout.Width(50));

            GUILayout.EndHorizontal();
        }

        GUILayout.Space(5);

        // Повторная кнопка отмены внизу
        if (GUILayout.Button("← Отмена", _smallButtonStyle))
        {
            _isSelectingLinkTarget = false;
            _selectingLinkIndex = -1;
        }
    }

    #endregion

    #region Group Editor

    private void DrawGroupEditor()
    {
        if (_editingGroup == null)
        {
            CloseEditor();
            return;
        }

        GUILayout.Label($"⚡ Редактирование группы: {_editingGroup.name}", _headerStyle);
        GUILayout.Space(5);

        if (GUILayout.Button("← Назад к списку", _buttonStyle))
        {
            CloseEditor();
            return;
        }

        GUILayout.Space(10);
        DrawSeparator();

        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

        // Основные настройки
        GUILayout.Label("⚙️ Настройки группы", _headerStyle);
        GUILayout.Space(5);

        // Паттерн
        GUILayout.BeginHorizontal();
        float scaleFactor = Screen.height / 1080f;
        scaleFactor = Mathf.Clamp(scaleFactor, 0.8f, 2f);

        GUILayout.Label("Паттерн:", _labelStyle, GUILayout.Width(150 * scaleFactor));
        _editingGroup.pattern = DrawEnumPopup(_editingGroup.pattern, GUILayout.Width(180));
        GUILayout.EndHorizontal();

        // Описание паттерна
        string patternDesc = _editingGroup.pattern switch
        {
            InstabilityPattern.Synchronized => "Все одновременно",
            InstabilityPattern.Sequential => "По очереди",
            InstabilityPattern.Random => "Случайный порядок",
            InstabilityPattern.Wave => "Волна →",
            InstabilityPattern.PingPong => "Волна ⟷",
            InstabilityPattern.Radial => "От центра",
            InstabilityPattern.Clustered => "Группами",
            InstabilityPattern.Domino => "Домино с ускорением",
            InstabilityPattern.Accelerating => "С ускорением",
            InstabilityPattern.Custom => "Пользовательский",
            _ => ""
        };
        GUILayout.Label($"  {patternDesc}", _labelStyle);

        GUILayout.Space(5);

        // Интервал
        GUILayout.BeginHorizontal();
        GUILayout.Label("Интервал:", _labelStyle, GUILayout.Width(150 * scaleFactor));
        string intervalStr = GUILayout.TextField(_editingGroup.switchStateInterval.ToString("F1"),
            GUILayout.Width(60));
        if (float.TryParse(intervalStr, out float newInterval))
        {
            _editingGroup.switchStateInterval = Mathf.Max(0.5f, newInterval);
        }

        GUILayout.Label("сек", _labelStyle);
        GUILayout.EndHorizontal();

        // Задержка между объектами (если нужна)
        if (_editingGroup.pattern == InstabilityPattern.Sequential ||
            _editingGroup.pattern == InstabilityPattern.Wave ||
            _editingGroup.pattern == InstabilityPattern.PingPong ||
            _editingGroup.pattern == InstabilityPattern.Radial ||
            _editingGroup.pattern == InstabilityPattern.Clustered ||
            _editingGroup.pattern == InstabilityPattern.Domino)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Задержка между:", _labelStyle, GUILayout.Width(150 * scaleFactor));
            string delayStr = GUILayout.TextField(_editingGroup.delayBetweenObjects.ToString("F2"),
                GUILayout.Width(60));
            if (float.TryParse(delayStr, out float newDelay))
            {
                _editingGroup.delayBetweenObjects = Mathf.Max(0, newDelay);
            }

            GUILayout.Label("сек", _labelStyle);
            GUILayout.EndHorizontal();
        }

        // Специфичные параметры
        if (_editingGroup.pattern == InstabilityPattern.Accelerating)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Мин. интервал:", _labelStyle, GUILayout.Width(150 * scaleFactor));
            string minStr = GUILayout.TextField(_editingGroup.minInterval.ToString("F1"), GUILayout.Width(60));
            if (float.TryParse(minStr, out float newMin))
            {
                _editingGroup.minInterval = Mathf.Max(0.3f, newMin);
            }

            GUILayout.Label("сек", _labelStyle);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Ускорение:", _labelStyle, GUILayout.Width(150 * scaleFactor));
            string accelStr = GUILayout.TextField(_editingGroup.accelerationRate.ToString("F2"), GUILayout.Width(60));
            if (float.TryParse(accelStr, out float newAccel))
            {
                _editingGroup.accelerationRate = Mathf.Max(0.01f, newAccel);
            }

            GUILayout.Label("сек/цикл", _labelStyle);
            GUILayout.EndHorizontal();
        }

        if (_editingGroup.pattern == InstabilityPattern.Clustered)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Размер кластера:", _labelStyle, GUILayout.Width(150 * scaleFactor));
            string clusterStr = GUILayout.TextField(_editingGroup.clusterSize.ToString(), GUILayout.Width(60));
            if (int.TryParse(clusterStr, out int newCluster))
            {
                _editingGroup.clusterSize = Mathf.Max(1, newCluster);
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);
        DrawSeparator();

        // Визуальная обратная связь
        GUILayout.Label("💡 Визуальная обратная связь", _headerStyle);
        GUILayout.Space(5);

        _editingGroup.showWarningEffect =
            GUILayout.Toggle(_editingGroup.showWarningEffect, " Показывать предупреждение", _toggleStyle);

        if (_editingGroup.showWarningEffect)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Время предупреждения:", _labelStyle, GUILayout.Width(150 * scaleFactor));
            string warnStr = GUILayout.TextField(_editingGroup.warningTime.ToString("F1"), GUILayout.Width(60));
            if (float.TryParse(warnStr, out float newWarn))
            {
                _editingGroup.warningTime = Mathf.Max(0, newWarn);
            }

            GUILayout.Label("сек", _labelStyle);
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);
        DrawSeparator();

        // Управление
        GUILayout.Label("🎮 Управление", _headerStyle);
        GUILayout.Space(5);

        GUILayout.Label($"Статус: {(_editingGroup.IsActive ? "✅ Активна" : "⏸ Остановлена")}", _labelStyle);

        GUILayout.BeginHorizontal();
        if (_editingGroup.IsActive)
        {
            if (GUILayout.Button("■ Остановить", _buttonStyle))
            {
                _editingGroup.StopDynamicStateSwitching();
            }
        }
        else
        {
            if (GUILayout.Button("▶ Запустить", _buttonStyle))
            {
                _editingGroup.StartDynamicStateSwitching();
            }
        }

        if (GUILayout.Button("⚡ Триггер сейчас", _buttonStyle))
        {
            _editingGroup.TriggerCycleNow();
        }

        GUILayout.EndHorizontal();

        if (GUILayout.Button("↺ Reset всех объектов", _buttonStyle))
        {
            _editingGroup.ResetAllToInitial();
        }

        GUILayout.Space(10);
        DrawSeparator();

        // Объекты группы
        GUILayout.Label($"📦 Объекты группы ({_editingGroup.Collapsibles.Count})", _headerStyle);
        GUILayout.Space(5);

        foreach (var c in _editingGroup.Collapsibles)
        {
            if (c == null) continue;

            GUILayout.BeginHorizontal();
            GUILayout.Label($"• {c.name}", _labelStyle);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Открыть", _smallButtonStyle, GUILayout.Width(70)))
            {
                _editingCollapsible = c;
                _editingGroup = null;
            }

            GUILayout.EndHorizontal();
        }

        if (GUILayout.Button("🔄 Обновить список из дочерних", _buttonStyle))
        {
            _editingGroup.SetCollapsiblesFromChildren();
        }

        GUILayout.EndScrollView();
    }

    #endregion

    #region Pages

    private void DrawOverviewPage()
    {
        GUILayout.Label("📊 Статистика сцены", _headerStyle);
        GUILayout.Space(5);

        int collapsibleCount = _allCollapsibles?.Length ?? 0;
        int puzzleCount = _allPuzzles?.Length ?? 0;
        int linkCount = _allLinks?.Length ?? 0;
        int groupCount = _allGroups?.Length ?? 0;

        DrawStatRow("Collapsible объектов", collapsibleCount.ToString());

        if (_allCollapsibles != null && collapsibleCount > 0)
        {
            int absolute = _allCollapsibles.Count(c => c.stabilityLevel == StabilityLevel.Absolute);
            int strong = _allCollapsibles.Count(c => c.stabilityLevel == StabilityLevel.Strong);
            int weak = _allCollapsibles.Count(c => c.stabilityLevel == StabilityLevel.Weak);
            int unstable = _allCollapsibles.Count(c => c.stabilityLevel == StabilityLevel.Unstable);

            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(
                $"🔒 {absolute}  |  🔗 {strong}  |  ✋ {weak}  |  ⚡ {unstable}",
                _labelStyle);
            GUILayout.EndHorizontal();

            int inOld = _allCollapsibles.Count(c => c.CurrentState == CollapseState.Old);
            int inNew = _allCollapsibles.Count(c => c.CurrentState == CollapseState.New);
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label($"Old: {inOld}  |  New: {inNew}", _labelStyle);
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10);
        DrawStatRow("Головоломок", puzzleCount.ToString());

        if (_allPuzzles != null && puzzleCount > 0)
        {
            int solved = _allPuzzles.Count(p => p.IsSolved);
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label($"✅ Решено: {solved}  |  ❌ Не решено: {puzzleCount - solved}", _labelStyle);
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(5);
        DrawStatRow("Контроллеров связей", linkCount.ToString());
        DrawStatRow("Групп нестабильности", groupCount.ToString());

        GUILayout.Space(20);
        GUILayout.Label("🎮 Глобальные действия", _headerStyle);
        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Всё → Old", _buttonStyle, GUILayout.Height(30)))
        {
            foreach (var c in _allCollapsibles)
            {
                if (c.CanBeChanged)
                    c.Collapse(CollapseOrigin.Script, CollapseState.Old);
            }
        }

        if (GUILayout.Button("Всё → New", _buttonStyle, GUILayout.Height(30)))
        {
            foreach (var c in _allCollapsibles)
            {
                if (c.CanBeChanged)
                    c.Collapse(CollapseOrigin.Script, CollapseState.New);
            }
        }

        GUILayout.EndHorizontal();

        if (GUILayout.Button("↺ Reset всех", _buttonStyle, GUILayout.Height(30)))
        {
            foreach (var c in _allCollapsibles)
            {
                c.Reset();
            }
        }

        GUILayout.Space(10);

        GUILayout.Label("💡 Подсказка:", _labelStyle);
        GUILayout.Label("Клик на метку над объектом в мире → редактирование", _labelStyle);
    }

    private void DrawObjectsListPage()
    {
        GUILayout.Label("📦 Список объектов", _headerStyle);
        GUILayout.Space(5);

        GUILayout.Label("Кликните на метку над объектом в мире для редактирования", _labelStyle);
        GUILayout.Space(10);

        if (_allCollapsibles == null || _allCollapsibles.Length == 0)
        {
            GUILayout.Label("Нет объектов", _labelStyle);
            return;
        }

        foreach (var c in _allCollapsibles)
        {
            if (c == null) continue;

            GUILayout.BeginHorizontal();

            string icon = c.stabilityLevel switch
            {
                StabilityLevel.Absolute => "🔒",
                StabilityLevel.Strong => "🔗",
                StabilityLevel.Weak => "✋",
                StabilityLevel.Unstable => "⚡",
                _ => "?"
            };

            if (GUILayout.Button($"{icon} {c.name}", _smallButtonStyle, GUILayout.Width(250)))
            {
                _editingCollapsible = c;
            }

            _statusStyle.normal.textColor = c.CurrentState == CollapseState.Old
                ? new Color(0.9f, 0.8f, 0.6f)
                : new Color(0.6f, 0.8f, 1f);
            GUILayout.Label($"[{c.CurrentState}]", _statusStyle);

            GUILayout.EndHorizontal();
        }
    }

    private void DrawPuzzlesPage()
    {
        GUILayout.Label("🧩 Головоломки", _headerStyle);
        GUILayout.Space(5);

        if (_allPuzzles == null || _allPuzzles.Length == 0)
        {
            GUILayout.Label("Нет головоломок", _labelStyle);
            return;
        }

        foreach (var puzzle in _allPuzzles)
        {
            if (puzzle == null) continue;

            GUILayout.BeginVertical(GUI.skin.box);

            string status = puzzle.IsSolved ? "✅" : "❌";
            _statusStyle.normal.textColor = puzzle.IsSolved
                ? new Color(0.4f, 1f, 0.5f)
                : new Color(1f, 0.9f, 0.4f);
            _statusStyle.fontStyle = FontStyle.Bold;
            GUILayout.Label($"{status} {puzzle.puzzleName}", _statusStyle);
            _statusStyle.fontStyle = FontStyle.Normal;

            _statusStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            GUILayout.Label($"Прогресс: {puzzle.Progress * 100:F0}%", _statusStyle);

            if (puzzle.conditions != null)
            {
                foreach (var cond in puzzle.conditions)
                {
                    if (cond.target == null) continue;

                    string condIcon = cond.IsSatisfied ? "✓" : "✗";
                    _statusStyle.normal.textColor = cond.IsSatisfied
                        ? new Color(0.5f, 1f, 0.5f)
                        : new Color(1f, 0.5f, 0.5f);

                    GUILayout.BeginHorizontal();
                    GUILayout.Label(
                        $"  {condIcon} {cond.target.name}: {cond.target.CurrentState} (нужно: {cond.requiredState})",
                        _statusStyle);

                    if (!cond.IsSatisfied && cond.target.CanBeChanged)
                    {
                        if (GUILayout.Button("Fix", _smallButtonStyle, GUILayout.Width(40)))
                        {
                            cond.target.Collapse(CollapseOrigin.Script, cond.requiredState);
                        }
                    }

                    GUILayout.EndHorizontal();
                }
            }

            if (!puzzle.IsSolved)
            {
                if (GUILayout.Button("✅ Автопрохождение", _buttonStyle))
                {
                    foreach (var cond in puzzle.conditions)
                    {
                        if (cond.target != null && cond.target.CanBeChanged)
                        {
                            cond.target.Collapse(CollapseOrigin.Script, cond.requiredState);
                        }
                    }
                }
            }

            GUILayout.EndVertical();
            GUILayout.Space(5);
        }
    }

    private void DrawGroupsPage()
    {
        GUILayout.Label("⚡ Группы нестабильности", _headerStyle);
        GUILayout.Space(5);

        if (_allGroups == null || _allGroups.Length == 0)
        {
            GUILayout.Label("Нет групп", _labelStyle);
            return;
        }

        foreach (var group in _allGroups)
        {
            if (group == null) continue;

            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button($"⚡ {group.name}", _buttonStyle, GUILayout.Width(250)))
            {
                _editingGroup = group;
            }

            _statusStyle.normal.textColor = group.IsActive
                ? new Color(0.4f, 1f, 0.4f)
                : new Color(0.7f, 0.7f, 0.7f);
            GUILayout.Label(group.IsActive ? "▶" : "■", _statusStyle);

            GUILayout.EndHorizontal();

            _statusStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);
            GUILayout.Label($"  {group.pattern} | {group.Collapsibles.Count} объектов", _statusStyle);

            GUILayout.EndVertical();
            GUILayout.Space(3);
        }
    }

    #endregion

    #region World Labels

    private void DrawWorldLabels()
    {
        if (_allCollapsibles == null) return;

        Camera cam = _playerCamera;
        if (cam == null) return;

        _labelRects.Clear();

        foreach (var c in _allCollapsibles)
        {
            if (c == null) continue;

            Vector3 worldPos = c.transform.position + Vector3.up * 0.2f;
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

            if (screenPos.z <= 0) continue;
            if (screenPos.x < 0 || screenPos.x > Screen.width) continue;
            if (screenPos.y < 0 || screenPos.y > Screen.height) continue;

            float distance = Vector3.Distance(cam.transform.position, c.transform.position);
            if (distance > maxLabelDistance) continue;

            float alpha = Mathf.Clamp01(1f - (distance - maxLabelDistance * 0.5f) / (maxLabelDistance * 0.5f));

            string icon = c.stabilityLevel switch
            {
                StabilityLevel.Absolute => "🔒",
                StabilityLevel.Strong => "🔗",
                StabilityLevel.Weak => "✋",
                StabilityLevel.Unstable => "⚡",
                _ => "?"
            };

            string label = $"{icon} {c.name}\n{c.CurrentState}";

            Color labelColor = c.GetStabilityColor();
            labelColor.a = alpha;

            // Подсветка если редактируем
            if (_editingCollapsible == c)
            {
                labelColor = Color.yellow;
                labelColor.a = 1f;
            }

            float scaleFactor = Screen.height / 1080f;
            scaleFactor = Mathf.Clamp(scaleFactor, 0.8f, 2f);

            // Подсветка если редактируем
            if (_editingCollapsible == c)
            {
                labelColor = new Color(1f, 0.9f, 0.3f); // Ярко-жёлтый
                labelColor.a = 1f;
            }

            int fontSize = Mathf.Max(12, Mathf.RoundToInt((18 - distance * 0.2f) * scaleFactor));

            GUIStyle worldStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = labelColor },
                fontStyle = FontStyle.Bold
            };

            float guiY = Screen.height - screenPos.y;
            Vector2 size = worldStyle.CalcSize(new GUIContent(label));
            Rect labelRect = new Rect(screenPos.x - size.x / 2, guiY - size.y, size.x, size.y);

            // Кликабельная зона (увеличена для удобства)
            Rect clickRect = new Rect(
                labelRect.x - labelClickRadius / 2 * scaleFactor,
                labelRect.y - labelClickRadius / 2 * scaleFactor,
                labelRect.width + labelClickRadius * scaleFactor,
                labelRect.height + labelClickRadius * scaleFactor);
            _labelRects[c] = clickRect;

            // Тень фона
            GUI.color = new Color(0, 0, 0, alpha * 0.8f);
            GUI.DrawTexture(
                new Rect(labelRect.x - 7, labelRect.y - 5, labelRect.width + 14, labelRect.height + 10),
                Texture2D.whiteTexture);

            // Фон с рамкой
            Color bgColor = _editingCollapsible == c
                ? new Color(0.3f, 0.5f, 0.1f, alpha * 0.9f)
                : new Color(0.05f, 0.05f, 0.08f, alpha * 0.85f);
            GUI.color = bgColor;
            GUI.DrawTexture(
                new Rect(labelRect.x - 5, labelRect.y - 3, labelRect.width + 10, labelRect.height + 6),
                Texture2D.whiteTexture);

            // Рамка
            GUI.color = _editingCollapsible == c
                ? new Color(1f, 0.9f, 0.3f, alpha)
                : labelColor;
            DrawBox(
                new Rect(labelRect.x - 5, labelRect.y - 3, labelRect.width + 10, labelRect.height + 6),
                2);

            GUI.color = Color.white;
            GUI.Label(labelRect, label, worldStyle);
        }
    }

    private void HandleLabelClicks()
    {
        Vector2 mousePos = Event.current.mousePosition;

        foreach (var kvp in _labelRects)
        {
            if (kvp.Value.Contains(mousePos))
            {
                _editingCollapsible = kvp.Key;
                _editingGroup = null;
                Event.current.Use();
                return;
            }
        }
    }

    #endregion

    #region Helpers

    private void DrawStatRow(string label, string value)
    {
        GUILayout.BeginHorizontal();
        _labelStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
        GUILayout.Label(label, _labelStyle, GUILayout.Width(200));
        _labelStyle.normal.textColor = Color.white;
        _labelStyle.fontStyle = FontStyle.Bold;
        GUILayout.Label(value, _labelStyle);
        _labelStyle.fontStyle = FontStyle.Normal;
        GUILayout.EndHorizontal();
    }

    private void DrawSeparator()
    {
        float scaleFactor = Screen.height / 1080f;
        scaleFactor = Mathf.Clamp(scaleFactor, 0.8f, 2f);

        GUILayout.Space(5 * scaleFactor);
        var rect = GUILayoutUtility.GetRect(1, 2 * scaleFactor, GUILayout.ExpandWidth(true));

        // Градиент для разделителя
        GUI.color = new Color(0.3f, 0.5f, 0.7f, 0.3f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUILayout.Space(5 * scaleFactor);
    }

    private T DrawEnumPopup<T>(T selected, params GUILayoutOption[] options) where T : System.Enum
    {
        string[] names = System.Enum.GetNames(typeof(T));
        int selectedIndex = System.Array.IndexOf(names, selected.ToString());

        int newIndex = GUILayout.SelectionGrid(selectedIndex, names, 1, options);

        if (newIndex >= 0 && newIndex < names.Length)
        {
            return (T)System.Enum.Parse(typeof(T), names[newIndex]);
        }

        return selected;
    }

    #endregion
}