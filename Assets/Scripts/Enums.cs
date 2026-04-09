public enum CollapseState
{
    New = 0,
    Old = 1
}

/// <summary>
/// Уровень стабильности объекта.
/// Определяет, кто и как может переключать объект.
/// </summary>
public enum StabilityLevel
{
    /// <summary>
    /// Нельзя изменить ничем. Определяет границы пространства.
    /// </summary>
    Absolute = 1,

    /// <summary>
    /// Игрок не может переключить напрямую. Только через запутанность.
    /// </summary>
    Strong = 2,

    /// <summary>
    /// Игрок может свободно переключать. Основной инструмент.
    /// </summary>
    Weak = 3,

    /// <summary>
    /// Переключается само по таймеру + игрок + запутанность.
    /// </summary>
    Unstable = 4
}

/// <summary>
/// Кто инициировал схлопывание.
/// </summary>
public enum CollapseOrigin
{
    Player,     // Игрок нажал кнопку
    Chain,      // Через запутанность (связь с другим объектом)
    Timer,      // Нестабильный объект по таймеру
    Script      // Вызвано из кода, Unity Event, или другой системы
}

/// <summary>
/// Условие срабатывания связи.
/// </summary>
public enum CollapseTriggerCondition
{
    /// <summary>При любом переключении источника</summary>
    OnAnyCollapse,

    /// <summary>Только когда источник переключается в Old</summary>
    OnCollapseToOld,

    /// <summary>Только когда источник переключается в New</summary>
    OnCollapseToNew,

    /// <summary>Только когда игрок переключает источник</summary>
    OnPlayerCollapse,

    /// <summary>Только когда источник переключён другой связью (цепочка)</summary>
    OnChainCollapse,

    /// <summary>Только когда источник переключён таймером</summary>
    OnTimerCollapse
}

/// <summary>
/// Действие, выполняемое над целевым объектом при срабатывании связи.
/// </summary>
public enum CollapseLinkAction
{
    /// <summary>Переключить состояние (toggle)</summary>
    Toggle,

    /// <summary>Всегда установить в Old</summary>
    SetToOld,

    /// <summary>Всегда установить в New</summary>
    SetToNew,

    /// <summary>Установить в то же состояние, что и источник</summary>
    MatchSource,

    /// <summary>Установить в противоположное состояние источника</summary>
    InvertSource
}

/// <summary>
/// Паттерн поведения нестабильных объектов в группе.
/// </summary>
public enum InstabilityPattern
{
    /// <summary>Все объекты переключаются одновременно</summary>
    Synchronized,

    /// <summary>Объекты переключаются по очереди с задержкой</summary>
    Sequential,

    /// <summary>Случайный порядок и случайный интервал</summary>
    Random,

    /// <summary>Волной от первого объекта к последнему</summary>
    Wave,

    /// <summary>Интервал уменьшается со временем (нарастающий хаос)</summary>
    Accelerating,

    /// <summary>Волной туда-обратно (ping-pong)</summary>
    PingPong,

    /// <summary>От центра к краям</summary>
    Radial,

    /// <summary>Группами (несколько объектов одновременно)</summary>
    Clustered,

    /// <summary>Каскадный эффект домино с возрастающей скоростью</summary>
    Domino,

    /// <summary>Пользовательская последовательность</summary>
    Custom
}