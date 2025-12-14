using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Outline), typeof(Dissolvable))]
public class COState : MonoBehaviour, ICollapsible
{
    public Collapsible parentCollapsible;
    [SerializeField] private Outline outline;
    [SerializeField] private Dissolvable dissolvable;
    public Outline Outline => outline;
    public Dissolvable Dissolvable => dissolvable;

    public bool isHighlightable = true;

    #region Validation

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

    #endregion

    #region Editor

    public void SetParentOutlineAndDissolve()
    {
        parentCollapsible = transform.parent.GetComponent<Collapsible>();
        outline = GetComponent<Outline>();
        dissolvable = GetComponent<Dissolvable>();
    }

    #endregion

    #region Setters

    public void SetHighlightable(bool highlightable)
    {
        if (isHighlightable != highlightable)
        {
            isHighlightable = highlightable;
            if (!isHighlightable) SetOutlineActive(false);
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

    #endregion

    #region Collapsible
    
    public void OnCollapse()
    {
        parentCollapsible.Collapse(true);
    }
    
    #endregion

    #region Highlightable

    public void OnHighlight()
    {
        SetOutlineActive(isHighlightable);
    }

    public void OnUnhighlight()
    {
        SetOutlineActive(false);
    }

    #endregion

    #region Активация этого состояния

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

    #endregion
}