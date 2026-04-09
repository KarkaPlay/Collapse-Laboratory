using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Outline), typeof(Dissolvable))]
public class COState : MonoBehaviour, ICollapsible
{
    public Collapsible parentCollapsible;
    [SerializeField] private Outline outline;
    [SerializeField] private Dissolvable dissolvable;
    public Outline Outline => outline;
    public Dissolvable Dissolvable => dissolvable;

    public bool isHighlightable = true;

    #region Validation

    private void Awake()
    {
        ValidateComponents();
    }

    private void ValidateComponents()
    {
        if (parentCollapsible == null)
            GameDebug.LogError($"COState {gameObject.name}: Parent collapsible is missing.");

        if (outline == null)
            GameDebug.LogError($"COState {gameObject.name}: Outline component is missing.");
    }

    #endregion

    #region Editor

    public void SetParentOutlineAndDissolve()
    {
        parentCollapsible = transform.parent.GetComponent<Collapsible>();
        outline = GetComponent<Outline>();
        dissolvable = GetComponent<Dissolvable>();
    }

    #endregion

    #region Setters

    public void SetHighlightable(bool highlightable)
    {
        if (isHighlightable != highlightable)
        {
            isHighlightable = highlightable;
            if (!isHighlightable) SetOutlineActive(false);
        }
    }

    public void SetOutlineColor(Color color) => outline.OutlineColor = color;

    public void SetOutlineActive(bool active)
    {
        if (outline.enabled != active)
        {
            outline.enabled = active;
        }
    }

    #endregion

    #region Collapsible

    public void OnCollapse()
    {
        // Вызывается когда игрок нажимает F на этом объекте
        parentCollapsible.CollapseByPlayer();
    }

    #endregion

    #region Highlightable

    public void OnHighlight()
    {
        if (!isHighlightable) return;

        // Цвет outline зависит от стабильности
        UpdateOutlineColor();
        SetOutlineActive(true);
    }

    public void OnUnhighlight()
    {
        SetOutlineActive(false);
    }

    /// <summary>
    /// Обновляет цвет outline на основе уровня стабильности родителя.
    /// </summary>
    private void UpdateOutlineColor()
    {
        if (parentCollapsible == null) return;

        Color color;

        // Пробуем получить цвет из настроек
        var settings = CollapseSettings.CollapseLabSettings.Instance;
        if (settings != null)
        {
            color = settings.GetOutlineColor(parentCollapsible.stabilityLevel);
        }
        else
        {
            // Fallback
            color = parentCollapsible.stabilityLevel switch
            {
                StabilityLevel.Absolute => new Color(0.5f, 0.5f, 0.5f),
                StabilityLevel.Strong => new Color(1f, 0.8f, 0f),
                StabilityLevel.Weak => new Color(0.3f, 0.7f, 1f),
                StabilityLevel.Unstable => new Color(1f, 0.3f, 0.3f),
                _ => Color.white
            };
        }

        SetOutlineColor(color);
    }

    #endregion

    #region Активация этого состояния

    public void Activate(bool active)
    {
        StartCoroutine(Activating(active));
    }

    private IEnumerator Activating(bool active)
    {
        SetHighlightable(false);

        if (active)
        {
            yield return StartCoroutine(dissolvable.Undissolving());

            // Highlightable только если объект не Absolute
            if (parentCollapsible.CanBeChanged)
            {
                SetHighlightable(true);
            }
        }
        else
        {
            yield return StartCoroutine(dissolvable.Dissolving());
        }
    }

    #endregion
}