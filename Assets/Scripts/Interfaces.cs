// Интерфейс для взаимодействия (E)
public interface IInteractable
{
    void OnInteract();
    void OnHighlight();  // Активирует Outline и подсказку E
    void OnUnhighlight();
}

// Интерфейс для схлопывания (F)
public interface ICollapsible
{
    void OnCollapse();
    void OnCollapseHighlight();  // Активирует Outline и подсказку F
    void OnCollapseUnhighlight();
}