using UnityEngine;

public class COStateChild : MonoBehaviour,ICollapsible
{
    public COState parentCOState;

    public void OnCollapse() => parentCOState.OnCollapse();

    public void OnHighlight() => parentCOState.OnHighlight();

    public void OnUnhighlight() => parentCOState.OnUnhighlight();

    private void Start()
    {
        if (parentCOState == null)
            Debug.LogError($"COStateChild {gameObject.name}: Не установлен parentCOState.", this.gameObject);
    }
}