using UnityEngine;

public class COStateChild : MonoBehaviour,ICollapsible
{
    public COState parentCOState;

    public void OnCollapse() => parentCOState.OnCollapse();

    public void OnHighlight() => parentCOState.OnHighlight();

    public void OnUnhighlight() => parentCOState.OnUnhighlight();
}