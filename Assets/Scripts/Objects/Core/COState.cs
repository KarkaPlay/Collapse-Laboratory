using UnityEngine;

public class COState : MonoBehaviour, ICollapsible
{
    public Collapsible parentCollapsible;
    public CollapseState thisState { get; private set; }
    private Outline outline;

    private void Awake()
    {
        outline = GetComponent<Outline>();
        parentCollapsible = GetComponentInParent<Collapsible>();
    }

    public void Acivate(bool active)
    {
        gameObject.SetActive(active);
    }

    public void OnCollapse()
    {
        parentCollapsible.Collapse();
    }

    public void OnCollapseHighlight()
    {
        outline.enabled = true;
    }

    public void OnCollapseUnhighlight()
    {
        outline.enabled = false;
    }
}
