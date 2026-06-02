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

    private Coroutine _activeRoutine;

    /// <summary>Идёт ли сейчас анимация растворения/проявления.</summary>
    public bool IsTransitioning => _activeRoutine != null;

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

    #region Dissolve

    public void Dissolve()
    {
        StartCoroutine(Dissolving());
    }

    public IEnumerator Dissolving()
    {
        // Прерываем предыдущий незавершённый переход, иначе две корутины пишут
        // в один и тот же материал (_Dissolve) и оставляют его в "невидимом" состоянии.
        if (_activeRoutine != null)
        {
            StopCoroutine(_activeRoutine);
            _activeRoutine = null;
        }

        _activeRoutine = StartCoroutine(DissolvingRoutine());
        yield return _activeRoutine;
    }

    private IEnumerator DissolvingRoutine()
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

        _activeRoutine = null;

        OnTransitionEnded.Invoke();
        OnDissolved.Invoke();
    }

    #endregion

    #region Undissolve

    public void Undissolve()
    {
        StartCoroutine(Undissolving());
    }

    public IEnumerator Undissolving()
    {
        if (_activeRoutine != null)
        {
            StopCoroutine(_activeRoutine);
            _activeRoutine = null;
        }

        _activeRoutine = StartCoroutine(UndissolvingRoutine());
        yield return _activeRoutine;
    }

    private IEnumerator UndissolvingRoutine()
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

        _activeRoutine = null;

        OnUndissolved.Invoke();
        OnTransitionEnded.Invoke();
    }

    #endregion

    #region Setters

    private void SetAllRenderers(List<Renderer> newRenderers, float amount)
    {
        foreach (var r in newRenderers)
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

    #endregion
}
