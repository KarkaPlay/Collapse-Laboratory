using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Outline))]
public abstract class Interactable : MonoBehaviour, IInteractable
{
    private Outline outline;

    public UnityEvent OnInteractEvent;

    void Awake()
    {
        outline = GetComponent<Outline>();
    }

    public virtual void OnInteract()
    {
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