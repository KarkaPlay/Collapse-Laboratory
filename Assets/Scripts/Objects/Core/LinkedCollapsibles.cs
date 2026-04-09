using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[Obsolete]
[RequireComponent(typeof(TrailMoving))]
public class LinkedCollapsibles : MonoBehaviour
{
    public List<Collapsible> linkedCollapsibles;

    public float trailMoveTime = 1f;

    public bool invokesChainReaction = false;

    private TrailMoving trailMoving;

    private void Awake()
    {
        trailMoving = GetComponent<TrailMoving>();
    }

    public void CollapseAllLinked()
    {
        StartCoroutine(CollapseAllLinkedCoroutine());
    }

    private IEnumerator CollapseAllLinkedCoroutine()
    {
        trailMoving.SetTimeToMove(trailMoveTime);
        foreach (var collapsible in linkedCollapsibles)
        {
            trailMoving.StartTrail(collapsible.transform);
            yield return new WaitForSeconds(trailMoveTime);
            //collapsible.Collapse(false, invokesChainReaction);
        }
    }
}
