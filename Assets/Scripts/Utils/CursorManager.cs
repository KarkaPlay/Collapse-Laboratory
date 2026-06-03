using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Управляет видимостью и блокировкой курсора в зависимости от текущей сцены.
/// В сценах меню курсор виден и разблокирован; в геймплейных — скрыт и заблокирован.
/// Самосоздаётся до загрузки первой сцены, поэтому ставить его в сцены вручную не нужно.
/// </summary>
public class CursorManager : MonoBehaviour
{
    private static CursorManager _instance;

    [Tooltip("Имена сцен, в которых курсор должен быть виден (меню, экраны паузы и т.п.)")]
    [SerializeField] private List<string> menuSceneNames = new() { "Menu", "MainMenu" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (_instance != null) return;

        var go = new GameObject(nameof(CursorManager));
        _instance = go.AddComponent<CursorManager>();
        DontDestroyOnLoad(go);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // Применяем состояние для сцены, которая уже активна на момент запуска.
        ApplyCursorState(SceneManager.GetActiveScene());
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        // При возврате фокуса в окно переустанавливаем корректное состояние,
        // иначе курсор может «всплыть» в геймплейной сцене.
        if (hasFocus)
            ApplyCursorState(SceneManager.GetActiveScene());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyCursorState(scene);
    }

    private void ApplyCursorState(Scene scene)
    {
        bool isMenu = IsMenuScene(scene.name);
        SetCursorVisible(isMenu);
    }

    private bool IsMenuScene(string sceneName)
    {
        foreach (var menuName in menuSceneNames)
        {
            if (string.Equals(sceneName, menuName, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static void SetCursorVisible(bool isVisible)
    {
        Cursor.visible = isVisible;
        Cursor.lockState = isVisible ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
