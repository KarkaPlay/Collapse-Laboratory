using UnityEngine;

public class Door : Interactable
{
    private static readonly int IsOpen = Animator.StringToHash("isOpen");
    public bool isOpen;
    public Animator animator;

    public override void OnInteract()
    {
        base.OnInteract();
        GameDebug.Log("Дверь не открывается. Издать звук");
    }

    public void SwitchState()
    {
        if (!gameObject.activeSelf) return;

        isOpen = !isOpen;
        animator.SetBool(IsOpen, isOpen);
    }

    public void Open()
    {
        isOpen = true;
        animator.SetBool(IsOpen, true);
    }

    public void Close()
    {
        isOpen = false;
        animator.SetBool(IsOpen, false);
    }    
}
