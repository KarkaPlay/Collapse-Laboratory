using UnityEngine;

public class COStateChild : MonoBehaviour,ICollapsible
{
    public COState parentCOState;

    public void OnCollapse()
    {
        parentCOState.OnCollapse();
    }

    public void OnCollapseHighlight()
    {
        parentCOState.OnCollapseHighlight();
    }

    public void OnCollapseUnhighlight()
    {
        parentCOState.OnCollapseUnhighlight();
    }
}