using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Outline), typeof(Dissolvable))]
public class COState : MonoBehaviour, ICollapsible
{
    public Collapsible parentCollapsible;
    [SerializeField] private Outline outline;
    [SerializeField] private Dissolvable dissolvable;
    public Outline Outline { get => outline; }
    public Dissolvable Dissolvable { get => dissolvable; }

    public bool isHighlightable = true;

    private void Awake()
    {
        ValidateComponents();
    }

    private void ValidateComponents()
    {
        if (parentCollapsible == null)
            GameDebug.LogError($"COState {gameObject.name}: Parent collapsible is missing.");

        if (outline == null)
            GameDebug.LogError($"COState {gameObject.name}: Outline component is missing.");
    }

    public void SetParentOutlineAndDissolve()
    {
        parentCollapsible = transform.parent.GetComponent<Collapsible>();
        outline = GetComponent<Outline>();
        dissolvable = GetComponent<Dissolvable>();
    }

    public void SetHighlightable(bool _isHighlightable)
    {
        if (isHighlightable != _isHighlightable)
        {
            isHighlightable = _isHighlightable;
            if (!isHighlightable) SetOutlineActive(false);
        }
    }

    public void Activate(bool active)
    {
        StartCoroutine(Activating(active));
    }

    private IEnumerator Activating(bool active)
    {
        parentCollapsible.SetCanPlayerCollapse(false);
        SetHighlightable(false);

        if (active)
        {
            dissolvable.Undissolve();
        }
        else
        {
            dissolvable.Dissolve();
        }

        yield return new WaitForSeconds(dissolvable.timeToDissolve);

        if (!parentCollapsible.isBroken)
        {
            parentCollapsible.SetCanPlayerCollapse(true);
            SetHighlightable(true); // Включаем подсветку для нового состояния
        }
    }

    public void SetOutlineColor(Color color) => outline.OutlineColor = color;

    public void SetOutlineActive(bool active)
    {
        if (outline.enabled != active)
        {
            outline.enabled = active;
        }
    }

    public void OnCollapse()
    {
        parentCollapsible.Collapse(true);
    }

    public void OnCollapseHighlight()
    {
        SetOutlineActive(isHighlightable);
    }

    public void OnCollapseUnhighlight()
    {
        SetOutlineActive(false);
    }
}
