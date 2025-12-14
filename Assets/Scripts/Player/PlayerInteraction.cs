using Objects;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Дальность взаимодействия")]
    public float interactionDistance = 5;

    private IHighlightable currentTarget;
    private IAnimatedCollapsible currentAnimatedTarget;

    public LayerMask interactableLayer;
    
    private float _animationDirection;

    void OnDrawGizmos()
    {
        Gizmos.color = currentTarget == null ? Color.yellow : Color.green;

        Gizmos.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * interactionDistance);
    }

    public void OnInteract()
    {
        (currentTarget as IInteractable)?.OnInteract();
    }

    public void OnAnimateScroll(InputValue value)
    {
        _animationDirection = value.Get<float>();
    }

    public void OnCollapse()
    {
        (currentTarget as ICollapsible)?.OnCollapse();
        ClearTarget();
    }

    void Update()
    {
        RaycastCheck();
        AnimateCollapsible();
    }

    private void RaycastCheck()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        if (Physics.Raycast(ray, out var hit, interactionDistance, interactableLayer))
        {
            if (hit.collider.TryGetComponent(out IHighlightable target))
            {
                if (currentTarget != target)
                {
                    ClearTarget();
                    currentTarget = target;
                    currentTarget.OnHighlight();
                    
                    currentAnimatedTarget = currentTarget as IAnimatedCollapsible;
                    PlayerUI.Instance.SetAnimatedTargetSliderVisible(currentAnimatedTarget != null);
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
            currentAnimatedTarget?.Animate(_animationDirection);
        }
    }

    private void ClearTarget()
    {
        currentTarget?.OnUnhighlight();
        currentTarget = null;
        currentAnimatedTarget = null;
        PlayerUI.Instance.SetAnimatedTargetSliderVisible(false);
    }
}
