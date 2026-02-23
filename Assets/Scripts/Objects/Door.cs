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
        SetAnimatorIsOpen(isOpen);
    }

    public void Open()
    {
        isOpen = true;
        SetAnimatorIsOpen(true);
    }

    public void Close()
    {
        isOpen = false;
        SetAnimatorIsOpen(false);
    }

    private void SetAnimatorIsOpen(bool newState)
    {
        if (animator)
        {
            animator.SetBool(IsOpen, newState);
        }
        else
        {
            Debug.LogWarning("Не настроен аниматор двери", gameObject);
        }
    }
}
