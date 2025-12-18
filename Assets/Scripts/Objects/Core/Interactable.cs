using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Outline))]
public abstract class Interactable : MonoBehaviour, IInteractable
{
    private Outline _outline;

    public bool canPlayerInteract = true;
    public bool isWorking = true;

    public UnityEvent OnInteractEvent;

    public void SetCanPlayerInteract(bool newState) => canPlayerInteract = newState;

    public void SetIsWorking(bool newState) => isWorking = newState;

    void Awake()
    {
        _outline = GetComponent<Outline>();
    }

    public virtual void OnInteract()
    {
        if (canPlayerInteract && isWorking)
        {
            OnInteractEvent?.Invoke();
        }
    }

    public virtual void OnHighlight()
    {
        _outline.enabled = true;
    }

    public virtual void OnUnhighlight()
    {
        _outline.enabled = false;
    }
    
    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (OnInteractEvent == null) return;
        
        var position = transform.position;
        
        // Draw lines to all objects in the OnInteractEvent
        for (int i = 0; i < OnInteractEvent.GetPersistentEventCount(); i++)
        {
            var target = OnInteractEvent.GetPersistentTarget(i);
            if (target != null)
            {
                // Get the target's GameObject
                var targetObject = target as UnityEngine.Object;
                var targetTransform = (target as Component)?.transform ?? (target as GameObject)?.transform;

                if (targetTransform != null)
                {

                    if (Selection.activeGameObject == gameObject)
                    {
                        // Use Handles for selected object (thicker line, draws on top)
                        Handles.color = new Color(0f, 1f, 0f, 0.8f); // Lighter green with transparency
                        Handles.DrawLine(position, targetTransform.position, 3f);

                        // Draw a small sphere at the target position
                        Handles.SphereHandleCap(0, targetTransform.position, Quaternion.identity, 0.2f,
                            EventType.Repaint);
                    }
                    else
                    {
                        // Use Gizmos for unselected objects
                        Gizmos.color = new Color(0f, 1f, 0f, 0.7f); // Slightly transparent green
                        Gizmos.DrawLine(position, targetTransform.position);
                        Gizmos.DrawWireCube(targetTransform.position, Vector3.one * 0.2f);
                    }
                }
            }
        }
#endif
    }
}