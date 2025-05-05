using System.Collections.Generic;
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
                collapsible.Collapse(false);
            }
        }
    }
}
