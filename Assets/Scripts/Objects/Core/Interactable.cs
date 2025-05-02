using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Outline))]
public abstract class Interactable : MonoBehaviour, IInteractable
{
    private Outline outline;

    public bool canPlayerInteract = true;

    public UnityEvent OnInteractEvent;

    public void SetCanPlayerInteract(bool newState)
    {
        canPlayerInteract = newState;
    }

    void Awake()
    {
        outline = GetComponent<Outline>();
    }

    public virtual void OnInteract()
    {
        if (!canPlayerInteract)
            return;
        OnInteractEvent.Invoke();
    }

    public virtual void OnHighlight()
    {
        outline.enabled = true;
    }

    public virtual void OnUnhighlight()
    {
        outline.enabled = false;
    }
}