using StarterAssets;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;

public class ReadableUI : SingletonBehaviour<ReadableUI>
{
    [Header("Main Panels")]
    public CanvasGroup overlayCanvasGroup;
    public RectTransform inspectionRect;   // Основная панель (база)
    public CanvasGroup contentCanvasGroup; // Панель с текстом (накладывается сверху)

    [Header("Animation Settings")]
    public float animationDuration = 0.4f;
    public AnimationCurve liftCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float hiddenYPosition = -1080f;
    public float visibleYPosition = 0f;

    [Header("Inspection Elements")]
    public Image objectImage;
    public Button readButton;
    public Button closeButton;

    [Header("Content Elements")]
    public TextMeshProUGUI bodyText;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI pageCounterText;
    public Button nextButton;
    public Button prevButton;
    public Button backToInspectionButton;

    private ReadableData _currentData;
    private int _currentPage = 0;
    private Action _onCloseCallback;
    private bool _isSystemOpen = false;

    private void Start()
    {
        // Начальное состояние
        overlayCanvasGroup.gameObject.SetActive(false);
        overlayCanvasGroup.alpha = 0;

        inspectionRect.anchoredPosition = new Vector2(0, hiddenYPosition);

        contentCanvasGroup.gameObject.SetActive(false);
        contentCanvasGroup.alpha = 0;

        // Подписка на кнопки
        readButton.onClick.AddListener(ShowContent);
        closeButton.onClick.AddListener(CloseSystem);
        backToInspectionButton.onClick.AddListener(HideContent);
        nextButton.onClick.AddListener(NextPage);
        prevButton.onClick.AddListener(PrevPage);
    }

    public void Open(ReadableData data, Action onClose = null)
    {
        if (_isSystemOpen) return;

        _currentData = data;
        _onCloseCallback = onClose;
        _currentPage = 0;
        _isSystemOpen = true;

        objectImage.sprite = _currentData.mainImage;
        overlayCanvasGroup.gameObject.SetActive(true);

        // Скрываем текстовую панель на случай, если она была открыта
        contentCanvasGroup.gameObject.SetActive(false);
        contentCanvasGroup.alpha = 0;

        StopAllCoroutines();
        StartCoroutine(FadeCanvasGroup(overlayCanvasGroup, 1f));
        StartCoroutine(AnimateLift(inspectionRect, visibleYPosition));

        TogglePlayerControl(false);
    }

    private void ShowContent()
    {
        titleText.text = _currentData.title;
        UpdatePage();

        contentCanvasGroup.gameObject.SetActive(true);
        StartCoroutine(FadeCanvasGroup(contentCanvasGroup, 1f));

        // Опционально: можно скрыть кнопки на основной панели, чтобы не просвечивали
        readButton.gameObject.SetActive(false);
        closeButton.gameObject.SetActive(false);
    }

    private void HideContent()
    {
        // Просто убираем панель текста, база (inspectionRect) остается на месте
        StartCoroutine(FadeCanvasGroup(contentCanvasGroup, 0f, () =>
        {
            contentCanvasGroup.gameObject.SetActive(false);
            readButton.gameObject.SetActive(true);
            closeButton.gameObject.SetActive(true);
        }));
    }

    private void CloseSystem()
    {
        _isSystemOpen = false;
        StopAllCoroutines();
        StartCoroutine(AnimateLift(inspectionRect, hiddenYPosition));
        StartCoroutine(FadeCanvasGroup(overlayCanvasGroup, 0f, () =>
        {
            overlayCanvasGroup.gameObject.SetActive(false);
            TogglePlayerControl(true);
            _onCloseCallback?.Invoke();
        }));
    }

    private IEnumerator AnimateLift(RectTransform target, float targetY)
    {
        float elapsed = 0;
        Vector2 startPos = target.anchoredPosition;
        Vector2 endPos = new Vector2(startPos.x, targetY);

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            float curvedT = liftCurve.Evaluate(t);
            target.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, curvedT);
            yield return null;
        }
        target.anchoredPosition = endPos;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float targetAlpha, Action onComplete = null)
    {
        float elapsed = 0;
        float duration = 0.25f;
        float startAlpha = cg.alpha;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }
        cg.alpha = targetAlpha;
        onComplete?.Invoke();
    }

    private void UpdatePage()
    {
        if (_currentData.pages.Length == 0) return;
        bodyText.text = _currentData.pages[_currentPage].text;
        pageCounterText.text = $"{_currentPage + 1} / {_currentData.pages.Length}";
        prevButton.gameObject.SetActive(_currentPage > 0);
        nextButton.gameObject.SetActive(_currentPage < _currentData.pages.Length - 1);
    }

    private void NextPage() { if (_currentPage < _currentData.pages.Length - 1) { _currentPage++; UpdatePage(); } }
    private void PrevPage() { if (_currentPage > 0) { _currentPage--; UpdatePage(); } }

    private void TogglePlayerControl(bool enabled)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        if (player.TryGetComponent<FirstPersonController>(out var controller)) controller.enabled = enabled;
        if (player.TryGetComponent<StarterAssetsInputs>(out var inputs))
        {
            inputs.enabled = enabled;
            Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !enabled;
        }
    }
}