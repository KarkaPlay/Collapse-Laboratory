using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Дальность взаимодействия")]
    public float interactionDistance = 5;

    private IHighlightable _currentTarget;
    private IAnimatedCollapsible _currentAnimatedTarget;

    public LayerMask interactableLayer;

    public Camera playerCamera;

    private float _animationDirection;

    void OnDrawGizmos()
    {
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
        Debug.Log("Collapse");
        (_currentTarget as ICollapsible)?.OnCollapse();
        ClearTarget();
    }

    void Update()
    {
        RaycastCheck();
        AnimateCollapsible();
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

    private void AnimateCollapsible()
    {
        if (_animationDirection != 0f)
        {
            // Игрок управляет анимацией
            _currentAnimatedTarget?.Animate(_animationDirection);
        }
        else
        {
            // Игрок отпустил клавишу — возвращаем автоматическое управление
            _currentAnimatedTarget?.StopPlayerControl();
        }
    }

    private void ClearTarget()
    {
        _currentTarget?.OnUnhighlight();
        _currentTarget = null;
        _currentAnimatedTarget?.StopPlayerControl();
        _currentAnimatedTarget = null;
        PlayerUI.Instance.SetAnimatedTargetSliderVisible(false);
    }
}
