using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class LinkedCollapsibles : MonoBehaviour
{
    public List<Collapsible> linkedCollapsibles;

    public void CollapseAllLinkedCollapsibles(Collapsible invokerCollapsible)
    {
        foreach (var collapsible in linkedCollapsibles)
        {
            if (collapsible != invokerCollapsible)
            {
                collapsible.Collapse(false, false);
            }
        }
    }

    #region Editor

#if UNITY_EDITOR
    
    private void OnDrawGizmos()
    {
        if (linkedCollapsibles == null || linkedCollapsibles.Count == 0) return;

        bool isSelected = Selection.activeGameObject == gameObject;
        var currentPosition = transform.position;

        foreach (var collapsible in linkedCollapsibles)
        {
            if (collapsible != null)
            {
                var targetPos = collapsible.transform.position;
                
                if (isSelected)
                {
                    // Use Handles for selected object (thicker line, draws on top)
                    Handles.color = new Color(1f, 0.5f, 0f, 0.8f); // Orange color
                    Handles.DrawLine(currentPosition, targetPos, 3f);

                    // Draw a small sphere at the target position
                    Handles.SphereHandleCap(0, targetPos, Quaternion.identity, 0.2f, EventType.Repaint);
                }
                else
                {
                    // Use Gizmos for unselected objects
                    Gizmos.color = new Color(1f, 0.5f, 0f, 0.7f); // Semi-transparent orange
                    Gizmos.DrawLine(currentPosition, targetPos);
                    Gizmos.DrawWireSphere(targetPos, 0.2f);
                }
            }
        }
    }
    
#endif

    #endregion
}
