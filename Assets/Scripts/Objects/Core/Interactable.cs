using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Outline))]
public abstract class Interactable : MonoBehaviour, IInteractable
{
    private Outline _outline;

    public bool canPlayerInteract = true;
    public bool isWorking = true;

    [Tooltip("Текст подсказки при наведении")]
    public string promptOverride = "[E] Взаимодействовать";

    public UnityEvent OnInteractEvent;

    public void SetCanPlayerInteract(bool newState) => canPlayerInteract = newState;

    public void SetIsWorking(bool newState) => isWorking = newState;

    void Awake()
    {
        _outline = GetComponent<Outline>();
    }

    public virtual void OnInteract()
    {
        if (canPlayerInteract && isWorking)
        {
            OnInteractEvent?.Invoke();
        }
    }

    public virtual void OnHighlight()
    {
        if (_outline != null)
            _outline.enabled = true;
    }

    public virtual void OnUnhighlight()
    {
        if (_outline != null)
            _outline.enabled = false;
    }
}