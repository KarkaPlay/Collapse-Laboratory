using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;

public class PlayerUI : SingletonBehaviour<PlayerUI>
{
    [Header("Animated Collapsible")]
    public Slider animatedCollapsibleSlider;

    [Header("Interaction Prompt")]
    [Tooltip("Текст подсказки взаимодействия (по центру экрана)")]
    public TextMeshProUGUI interactionPromptText;

    [Tooltip("Иконка типа взаимодействия")]
    public Image interactionIcon;

    [Tooltip("Фон подсказки")]
    public Image promptBackground;

    [Tooltip("Точка (crosshair) в центре экрана")]
    public GameObject crosshair;

    [Header("Спрайты иконок")]
    public Sprite iconCollapse;
    public Sprite iconInteract;
    public Sprite iconAnimate;
    public Sprite iconLocked;

    [Header("Stability Indicator")]
    [Tooltip("Визуальный индикатор уровня стабильности")]
    public Image stabilityIndicator;

    [Tooltip("Текст описания стабильности")]
    public TextMeshProUGUI stabilityText;

    [Header("Link Hint")]
    [Tooltip("Подсказка о связях")]
    public GameObject linkHintPanel;

    [Tooltip("Текст подсказки о связях")]
    public TextMeshProUGUI linkHintText;

    [Header("Warning Effect")]
    [Tooltip("Предупреждение о нестабильности")]
    public GameObject warningPanel;

    [Tooltip("Пульсирующая рамка предупреждения")]
    public Image warningBorder;

    [Tooltip("Максимальная непрозрачность предупреждения (1 = полностью перекрывает экран)")]
    [Range(0f, 1f)]
    public float warningMaxAlpha = 0.35f;

    [Tooltip("Минимальная непрозрачность в нижней точке пульсации")]
    [Range(0f, 1f)]
    public float warningMinAlpha = 0f;

    [Tooltip("Скорость пульсации (полных циклов в секунду)")]
    [Range(0.1f, 3f)]
    public float warningPulseSpeed = 1f;

    private Coroutine _warningPulseCoroutine;

    private void Start()
    {
        CheckAllElements();
        HideAllPrompts();
    }

    private void CheckAllElements()
    {
        if (animatedCollapsibleSlider == null)
            Debug.LogWarning("[PlayerUI] animatedCollapsibleSlider не назначен");
        if (interactionPromptText == null)
            Debug.LogWarning("[PlayerUI] interactionPromptText не назначен");
    }

    #region Animated Collapsible Slider

    public void UpdateAnimatedCollapsibleSlider(float value)
    {
        if (animatedCollapsibleSlider != null)
            animatedCollapsibleSlider.value = value;
    }

    public void SetAnimatedTargetSliderVisible(bool isVisible)
    {
        if (animatedCollapsibleSlider != null)
            animatedCollapsibleSlider.gameObject.SetActive(isVisible);
    }

    #endregion

    #region Interaction Prompt

    /// <summary>
    /// Показать подсказку взаимодействия с иконкой и цветом стабильности.
    /// </summary>
    public void ShowInteractionPrompt(string text, bool canInteract = true, StabilityLevel? stability = null,
        InteractionType type = InteractionType.Collapse)
    {
        if (interactionPromptText == null) return;

        interactionPromptText.gameObject.SetActive(true);
        interactionPromptText.text = text;

        // Цвет текста
        Color textColor = canInteract
            ? new Color(1f, 1f, 1f, 0.95f)
            : new Color(0.7f, 0.4f, 0.4f, 0.8f);
        interactionPromptText.color = textColor;

        // Иконка
        if (interactionIcon != null)
        {
            interactionIcon.gameObject.SetActive(true);
            interactionIcon.sprite = type switch
            {
                InteractionType.Collapse => iconCollapse,
                InteractionType.Interact => iconInteract,
                InteractionType.Animate => iconAnimate,
                InteractionType.Locked => iconLocked,
                _ => null
            };
            interactionIcon.color = textColor;
        }

        // Фон
        if (promptBackground != null)
        {
            promptBackground.gameObject.SetActive(true);
            Color bgColor = canInteract
                ? new Color(0.1f, 0.1f, 0.1f, 0.7f)
                : new Color(0.3f, 0.1f, 0.1f, 0.7f);
            promptBackground.color = bgColor;
        }

        // Индикатор стабильности
        if (stability.HasValue && stabilityIndicator != null)
        {
            ShowStabilityIndicator(stability.Value);
        }
        else
        {
            HideStabilityIndicator();
        }
    }

    public void HideInteractionPrompt()
    {
        if (interactionPromptText != null)
            interactionPromptText.gameObject.SetActive(false);
        if (interactionIcon != null)
            interactionIcon.gameObject.SetActive(false);
        if (promptBackground != null)
            promptBackground.gameObject.SetActive(false);

        HideStabilityIndicator();
    }

    #endregion

    #region Stability Indicator

    public void ShowStabilityIndicator(StabilityLevel level)
    {
        if (stabilityIndicator == null) return;

        stabilityIndicator.gameObject.SetActive(true);

        // Цвет по уровню стабильности
        Color color = level switch
        {
            StabilityLevel.Absolute => new Color(0.5f, 0.5f, 0.5f),
            StabilityLevel.Strong => new Color(1f, 0.8f, 0f),
            StabilityLevel.Weak => new Color(0.3f, 0.7f, 1f),
            StabilityLevel.Unstable => new Color(1f, 0.3f, 0.3f),
            _ => Color.white
        };

        stabilityIndicator.color = color;

        // Текст описания
        if (stabilityText != null)
        {
            stabilityText.gameObject.SetActive(true);
            stabilityText.text = level switch
            {
                StabilityLevel.Absolute => "СТАБИЛЕН",
                StabilityLevel.Strong => "СВЯЗАН",
                StabilityLevel.Weak => "СВОБОДЕН",
                StabilityLevel.Unstable => "НЕСТАБИЛЕН!",
                _ => ""
            };
            stabilityText.color = color;
        }
    }

    public void HideStabilityIndicator()
    {
        if (stabilityIndicator != null)
            stabilityIndicator.gameObject.SetActive(false);
        if (stabilityText != null)
            stabilityText.gameObject.SetActive(false);
    }

    #endregion

    #region Link Hint

    /// <summary>
    /// Показать подсказку о связях объекта.
    /// </summary>
    public void ShowLinkHint(string linkInfo)
    {
        if (linkHintPanel == null || linkHintText == null) return;

        linkHintPanel.SetActive(true);
        linkHintText.text = $"🔗 Связи:\n{linkInfo}";
    }

    public void HideLinkHint()
    {
        if (linkHintPanel != null)
            linkHintPanel.SetActive(false);
    }

    #endregion

    #region Warning Effect

    /// <summary>
    /// Показать предупреждение о нестабильности (пульсирующая рамка).
    /// </summary>
    public void ShowWarning()
    {
        if (warningPanel == null || warningBorder == null) return;

        warningPanel.SetActive(true);

        if (_warningPulseCoroutine != null)
            StopCoroutine(_warningPulseCoroutine);

        _warningPulseCoroutine = StartCoroutine(PulseWarning());
    }

    public void HideWarning()
    {
        if (warningPanel != null)
            warningPanel.SetActive(false);

        if (_warningPulseCoroutine != null)
        {
            StopCoroutine(_warningPulseCoroutine);
            _warningPulseCoroutine = null;
        }

        // Сбрасываем альфу, чтобы рамка не осталась "застывшей" подсвеченной,
        // если корутину остановили на пике пульсации.
        if (warningBorder != null)
        {
            Color c = warningBorder.color;
            c.a = warningMinAlpha;
            warningBorder.color = c;
        }
    }

    private IEnumerator PulseWarning()
    {
        float time = 0f;

        while (true)
        {
            time += Time.deltaTime * warningPulseSpeed;

            // Плавная синусоида в диапазоне [0..1]. Полный цикл за один период,
            // поэтому warningPulseSpeed задаёт частоту в циклах в секунду.
            float t = (Mathf.Sin(time * Mathf.PI * 2f) + 1f) * 0.5f;
            float alpha = Mathf.Lerp(warningMinAlpha, warningMaxAlpha, t);

            Color c = warningBorder.color;
            c.a = alpha;
            warningBorder.color = c;
            yield return null;
        }
    }

    #endregion

    #region Helper

    private void HideAllPrompts()
    {
        HideInteractionPrompt();
        HideLinkHint();
        HideWarning();
        SetAnimatedTargetSliderVisible(false);
    }

    #endregion
}

/// <summary>
/// Тип взаимодействия для выбора иконки.
/// </summary>
public enum InteractionType
{
    Collapse,
    Interact,
    Animate,
    Locked
}
