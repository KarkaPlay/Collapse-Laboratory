using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Dissolvable : MonoBehaviour
{
    public List<Renderer> renderers = new();
    public List<Collider> colliders = new();

    public float timeToDissolve = 1;

    public UnityEvent OnTransitionStarted;
    public UnityEvent OnTransitionEnded;
    public UnityEvent OnDissolved;
    public UnityEvent OnUndissolved;

#region Editor
    public void SetRendererThis()
    {
        renderers.Add(GetComponent<Renderer>());
    }

    public void SetRenderersInChildren()
    {
        renderers = new List<Renderer>(GetComponentsInChildren<Renderer>());
    }

    public void SetColliderThis()
    {
        colliders.Add(GetComponent<Collider>());
    }

    public void SetCollidersInChildren()
    {
        colliders.Clear();
        colliders.AddRange(GetComponentsInChildren<Collider>());
    }
#endregion

    public void Dissolve()
    {
        StartCoroutine(Dissolving());
    }

    public void Undissolve()
    {
        StartCoroutine(Undissolving());
    }

    IEnumerator Dissolving()
    {
        OnTransitionStarted.Invoke();

        
        SetRenderersActive(true);

        for (float i = 0; i < timeToDissolve; i += Time.deltaTime)
        {
            SetAllRenderers(renderers, i / timeToDissolve);
            yield return null;
        }

        SetAllRenderers(renderers, 1);

        SetRenderersActive(false);
        SetCollidersActive(false);

        OnTransitionEnded.Invoke();
        OnDissolved.Invoke();
    }

    IEnumerator Undissolving()
    {
        OnTransitionStarted.Invoke();

        SetRenderersActive(true);

        for (float i = timeToDissolve; i > 0; i -= Time.deltaTime)
        {
            SetAllRenderers(renderers, i / timeToDissolve);
            yield return null;
        }

        SetAllRenderers(renderers, 0);
        SetCollidersActive(true);

        OnUndissolved.Invoke();
        OnTransitionEnded.Invoke();
    }

    private void SetAllRenderers(List<Renderer> renderers, float amount)
    {
        foreach (var r in renderers)
        {
            r.material.SetFloat("_Dissolve", amount);
        }
    }

    private void SetRenderersActive(bool active)
    {
        foreach (var r in renderers)
        {
            r.enabled = active;
        }
    }

    private void SetCollidersActive(bool active)
    {
        foreach (var c in colliders)
        {
            c.enabled = active;
        }
    }
}