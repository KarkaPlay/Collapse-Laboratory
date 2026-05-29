using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Дальность взаимодействия")]
    public float interactionDistance = 5;

    private IHighlightable _currentTarget;
    private IAnimatedCollapsible _currentAnimatedTarget;
    private Collapsible _currentCollapsible; // Для доп. информации

    public LayerMask interactableLayer;
    public Camera playerCamera;

    private float _animationDirection;

    // Для предупреждения о нестабильности
    private bool _inUnstableZone = false;

    void OnDrawGizmos()
    {
        if (playerCamera == null) return;
        Gizmos.color = _currentTarget == null ? Color.yellow : Color.green;
        Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * interactionDistance);
    }

    public void OnInteract()
    {
        (_currentTarget as IInteractable)?.OnInteract();
    }

    public void OnAnimateScroll(InputValue value)
    {
        _animationDirection = value.Get<float>();
    }

    public void OnCollapse()
    {
        (_currentTarget as ICollapsible)?.OnCollapse();
        ClearTarget();
    }

    void Update()
    {
        RaycastCheck();
        AnimateCollapsible();
        CheckUnstableZone();
    }

    private void RaycastCheck()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out var hit, interactionDistance, interactableLayer))
        {
            if (hit.collider.TryGetComponent(out IHighlightable target))
            {
                if (_currentTarget != target)
                {
                    ClearTarget();
                    _currentTarget = target;
                    _currentTarget.OnHighlight();

                    _currentAnimatedTarget = _currentTarget as IAnimatedCollapsible;
                    PlayerUI.Instance.SetAnimatedTargetSliderVisible(_currentAnimatedTarget != null);

                    // Сохраняем Collapsible для доп. информации
                    if (_currentTarget is COState coState)
                        _currentCollapsible = coState.parentCollapsible;
                    else if (_currentTarget is COStateChild coStateChild)
                        _currentCollapsible = coStateChild.parentCOState?.parentCollapsible;
                    else
                        _currentCollapsible = null;

                    UpdateInteractionPrompt();
                }
            }
            else
            {
                ClearTarget();
            }
        }
        else
        {
            ClearTarget();
        }
    }

    private void UpdateInteractionPrompt()
    {
        if (_currentTarget == null)
        {
            PlayerUI.Instance.HideInteractionPrompt();
            PlayerUI.Instance.HideLinkHint();
            return;
        }

        // Определяем тип взаимодействия
        if (_currentTarget is ICollapsible)
        {
            if (_currentCollapsible != null)
            {
                string prompt;
                bool canInteract = _currentCollapsible.CanPlayerCollapse;
                InteractionType iconType;

                switch (_currentCollapsible.stabilityLevel)
                {
                    case StabilityLevel.Absolute:
                        prompt = "Абсолютно стабилен — нельзя изменить";
                        iconType = InteractionType.Locked;
                        canInteract = false;
                        break;

                    case StabilityLevel.Strong:
                        prompt = "Сильно стабилен — найдите связь";
                        iconType = InteractionType.Locked;
                        canInteract = false;
                        break;

                    case StabilityLevel.Weak:
                        prompt = "[F] Схлопнуть";
                        iconType = InteractionType.Collapse;
                        break;

                    case StabilityLevel.Unstable:
                        prompt = "[F] Схлопнуть (НЕСТАБИЛЕН!)";
                        iconType = InteractionType.Collapse;
                        break;

                    default:
                        prompt = "[F] Схлопнуть";
                        iconType = InteractionType.Collapse;
                        break;
                }

                PlayerUI.Instance.ShowInteractionPrompt(prompt, canInteract, _currentCollapsible.stabilityLevel,
                    iconType);

                // Показываем подсказку о связях
                ShowLinkHintIfAny();
            }
            else
            {
                PlayerUI.Instance.ShowInteractionPrompt("[F] Схлопнуть", true, null, InteractionType.Collapse);
            }
        }
        else if (_currentTarget is IInteractable)
        {
            // Используем кастомный текст из Interactable если он есть
            string promptText = "[E] Взаимодействовать";
            if (_currentTarget is Interactable interactable)
            {
                promptText = interactable.promptOverride;
            }

            PlayerUI.Instance.ShowInteractionPrompt(promptText, true, null, InteractionType.Interact);
            PlayerUI.Instance.HideLinkHint();
        }
        else if (_currentTarget is IAnimatedCollapsible)
        {
            PlayerUI.Instance.ShowInteractionPrompt("[Z/X] Управлять", true, null, InteractionType.Animate);
            PlayerUI.Instance.HideLinkHint();
        }
    }

    /// <summary>
    /// Показывает подсказку о связях, если у объекта есть CollapseLinkController.
    /// </summary>
    private void ShowLinkHintIfAny()
    {
        if (_currentCollapsible == null) return;

        var linkController = _currentCollapsible.GetComponent<CollapseLinkController>();
        if (linkController != null && linkController.ActiveLinkCount > 0)
        {
            var descriptions = linkController.GetLinkDescriptions();
            string hintText = string.Join("\n", descriptions);
            PlayerUI.Instance.ShowLinkHint(hintText);
        }
        else
        {
            PlayerUI.Instance.HideLinkHint();
        }
    }

    private void AnimateCollapsible()
    {
        if (_animationDirection != 0f)
        {
            _currentAnimatedTarget?.Animate(_animationDirection);
        }
        else
        {
            _currentAnimatedTarget?.StopPlayerControl();
        }
    }

    /// <summary>
    /// Проверяет, находится ли игрок в зоне нестабильности.
    /// Показывает предупреждение на UI.
    /// </summary>
    private void CheckUnstableZone()
    {
        // Простая проверка: ищем ближайший нестабильный объект в радиусе
        var unstableObjects = FindObjectsByType<Collapsible>(FindObjectsSortMode.None);
        bool nearUnstable = false;

        foreach (var obj in unstableObjects)
        {
            if (obj.stabilityLevel == StabilityLevel.Unstable)
            {
                float distance = Vector3.Distance(transform.position, obj.transform.position);
                if (distance < 5f) // Радиус предупреждения
                {
                    nearUnstable = true;
                    break;
                }
            }
        }

        if (nearUnstable && !_inUnstableZone)
        {
            _inUnstableZone = true;
            PlayerUI.Instance.ShowWarning();
        }
        else if (!nearUnstable && _inUnstableZone)
        {
            _inUnstableZone = false;
            PlayerUI.Instance.HideWarning();
        }
    }

    private void ClearTarget()
    {
        _currentTarget?.OnUnhighlight();
        _currentTarget = null;
        _currentAnimatedTarget?.StopPlayerControl();
        _currentAnimatedTarget = null;
        _currentCollapsible = null;
        PlayerUI.Instance.SetAnimatedTargetSliderVisible(false);
        PlayerUI.Instance.HideInteractionPrompt();
        PlayerUI.Instance.HideLinkHint();
    }
}
