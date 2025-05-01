using UnityEngine;

public class Door : Interactable
{
    public bool isOpen;
    public Animator animator;

    public override void OnInteract()
    {
        base.OnInteract();
        GameDebug.Log("Дверь не открывается. Издать звук");
    }

    public void SwitchState()
    {
        isOpen = !isOpen;
        animator.SetBool("isOpen", isOpen);
    }

    public void Open()
    {
        isOpen = true;
        animator.SetBool("isOpen", true);
    }

    public void Close()
    {
        isOpen = false;
        animator.SetBool("isOpen", false);
    }    
}
