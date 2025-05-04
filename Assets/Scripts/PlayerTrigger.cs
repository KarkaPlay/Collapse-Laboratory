using UnityEngine;
using UnityEngine.Events;

public class PlayerTrigger : MonoBehaviour
{
    public bool isPlayerInTrigger = false;

    public UnityEvent OnPlayerEnter;
    public UnityEvent OnPlayerExit;

    void OnTriggerEnter(Collider other)
    {
        OnPlayerEnter.Invoke();
        isPlayerInTrigger = true;
    }

    void OnTriggerExit(Collider other)
    {
        OnPlayerExit.Invoke();
        isPlayerInTrigger = false;
    }
}
