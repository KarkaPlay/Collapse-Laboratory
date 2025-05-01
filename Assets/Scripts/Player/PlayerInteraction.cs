using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Дальность взаимодействия")]
    public float interactionDistance = 5;

    [SerializeField] private IInteractable currentInteractable;
    [SerializeField] private ICollapsible currentCollapsible;

    void OnDrawGizmos()
    {
        Gizmos.color = (currentInteractable == null && currentCollapsible == null) ? Color.yellow : Color.green;

        Gizmos.DrawRay(Camera.main.transform.position, Camera.main.transform.forward * interactionDistance);
    }

    public void OnInteract()
    {
        currentInteractable?.OnInteract();
    }

    public void OnCollapse()
    {
        currentCollapsible?.OnCollapse();
    }

    void Update()
    {
        RaycastCheck();
    }

    private void RaycastCheck()
    {
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        if (Physics.Raycast(ray, out var hit, interactionDistance))
        {
            // Ищем IInteractable
            if (hit.collider.TryGetComponent(out IInteractable interactable))
            {
                if (currentInteractable != interactable)
                {
                    ClearInteractable();
                    currentInteractable = interactable;
                    currentInteractable.OnHighlight();
                }
            }
            else
            {
                ClearInteractable();
            }

            // Ищем ICollapsible
            if (hit.collider.TryGetComponent(out ICollapsible collapsible))
            {
                if (currentCollapsible != collapsible)
                {
                    ClearCollapsible();
                    currentCollapsible = collapsible;
                    currentCollapsible.OnCollapseHighlight();
                }
            }
            else
            {
                ClearCollapsible();
            }
        }
        else
        {
            ClearAllCurrents();
        }
    }

    private void ClearCollapsible()
    {
        if (currentCollapsible != null)
        {
            currentCollapsible.OnCollapseUnhighlight();
            currentCollapsible = null;
        }
    }

    private void ClearInteractable()
    {
        if (currentInteractable != null)
        {
            currentInteractable.OnUnhighlight();
            currentInteractable = null;
        }
    }

    private void ClearAllCurrents()
    {
        ClearInteractable();
        ClearCollapsible();
    }
}
