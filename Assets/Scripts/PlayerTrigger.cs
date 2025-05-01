using UnityEngine;
using UnityEngine.Events;

public class PlayerTrigger : MonoBehaviour
{
    public UnityEvent OnPlayerEnter;

    void OnTriggerEnter(Collider other)
    {
        OnPlayerEnter.Invoke();
    }
}
