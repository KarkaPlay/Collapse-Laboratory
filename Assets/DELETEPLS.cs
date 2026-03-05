using UnityEngine;
using UnityEngine.InputSystem;

public class DELETEPLS : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip clip;

    private void Update()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
