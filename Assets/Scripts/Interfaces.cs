public interface IHighlightable
{
    void OnHighlight();
    void OnUnhighlight();
}

// Интерфейс для взаимодействия (E)
public interface IInteractable : IHighlightable
{
    void OnInteract();
}

// Интерфейс для схлопывания (F)
public interface ICollapsible : IHighlightable
{
    void OnCollapse();
}

// Интерфейс для анимированных объектов (Z и X)
public interface IAnimatedCollapsible : IHighlightable
{
    void Animate(float directionMultiplier);

    void StopPlayerControl();
}