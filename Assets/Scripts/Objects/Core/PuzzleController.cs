using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Контроллер головоломки. Отслеживает состояния объектов и проверяет условия решения.
/// Размещается на пустом GameObject в сцене.
/// </summary>
public class PuzzleController : MonoBehaviour
{
    [Header("Информация")]
    [Tooltip("Название головоломки (для дизайнера)")]
    public string puzzleName = "Новая головоломка";

    [Tooltip("Описание головоломки (для дизайнера)")]
    [TextArea(2, 5)]
    public string puzzleDescription;

    [Header("Условия решения")]
    [Tooltip("Все условия должны быть выполнены одновременно")]
    public List<CollapseCondition> conditions = new();

    [Header("Настройки")]
    [Tooltip("Можно ли «рассрешить» головоломку (если состояние сбилось)")]
    public bool canBeUnsolved = true;

    [Tooltip("Сколько раз головоломка может быть решена (0 = без ограничений)")]
    public int maxSolveCount = 0;

    [Header("События")]
    public UnityEvent OnPuzzleSolved;
    public UnityEvent OnPuzzleUnsolved;
    public UnityEvent<float> OnProgressChanged; // 0..1 прогресс

    [Header("Состояние (только для чтения)")]
    [SerializeField] private bool _isSolved;
    [SerializeField] private int _solveCount;
    [SerializeField] private int _satisfiedConditions;

    public bool IsSolved => _isSolved;
    public int SolveCount => _solveCount;

    /// <summary>Прогресс решения от 0 до 1</summary>
    public float Progress => conditions.Count > 0
        ? (float)_satisfiedConditions / conditions.Count
        : 0f;

    private void OnEnable()
    {
        // Подписываемся на все объекты из условий
        foreach (var condition in conditions)
        {
            if (condition.target != null)
            {
                condition.target.OnCollapse.AddListener(OnAnyCollapseChanged);
            }
        }

        // Проверяем начальное состояние
        CheckConditions();
    }

    private void OnDisable()
    {
        foreach (var condition in conditions)
        {
            if (condition.target != null)
            {
                condition.target.OnCollapse.RemoveListener(OnAnyCollapseChanged);
            }
        }
    }

    private void OnAnyCollapseChanged(CollapseEventData _)
    {
        CheckConditions();
    }

    private void CheckConditions()
    {
        _satisfiedConditions = conditions.Count(c => c.IsSatisfied);
        bool allMet = _satisfiedConditions == conditions.Count && conditions.Count > 0;

        float progress = Progress;
        OnProgressChanged?.Invoke(progress);

        if (allMet && !_isSolved)
        {
            // Проверка лимита решений
            if (maxSolveCount > 0 && _solveCount >= maxSolveCount)
            {
                return;
            }

            _isSolved = true;
            _solveCount++;
            GameDebug.Log($"[PuzzleController] \"{puzzleName}\" SOLVED! (count: {_solveCount})");
            OnPuzzleSolved?.Invoke();
        }
        else if (!allMet && _isSolved && canBeUnsolved)
        {
            _isSolved = false;
            GameDebug.Log($"[PuzzleController] \"{puzzleName}\" UNSOLVED!");
            OnPuzzleUnsolved?.Invoke();
        }
    }

    /// <summary>
    /// Принудительно пересчитать условия (например, после загрузки).
    /// </summary>
    public void ForceCheck()
    {
        CheckConditions();
    }

    /// <summary>
    /// Получить подробный статус для дебаг-оверлея.
    /// </summary>
    public string GetDetailedStatus()
    {
        var status = $"=== {puzzleName} ===\n";
        status += $"Решена: {(_isSolved ? "ДА ✓" : "НЕТ ✗")}\n";
        status += $"Прогресс: {_satisfiedConditions}/{conditions.Count}\n\n";

        for (int i = 0; i < conditions.Count; i++)
        {
            var c = conditions[i];
            string satisfied = c.IsSatisfied ? "✓" : "✗";
            string targetName = c.target != null ? c.target.name : "(не назначен)";
            string currentState = c.target != null ? c.target.CurrentState.ToString() : "?";
            status += $"  {satisfied} {targetName}: {currentState} (нужно: {c.requiredState})\n";
        }

        return status;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Иконка головоломки
        Color puzzleColor = _isSolved
            ? new Color(0.2f, 1f, 0.3f, 0.8f)
            : new Color(1f, 0.8f, 0.2f, 0.8f);

        Gizmos.color = puzzleColor;
        Gizmos.DrawWireSphere(transform.position, 0.4f);

        // Линии к условиям
        foreach (var condition in conditions)
        {
            if (condition.target == null) continue;

            Color lineColor = condition.IsSatisfied
                ? new Color(0.2f, 1f, 0.3f, 0.4f)
                : new Color(1f, 0.3f, 0.3f, 0.4f);

            bool isSelected = UnityEditor.Selection.activeGameObject == gameObject;

            if (isSelected)
            {
                UnityEditor.Handles.color = lineColor;
                UnityEditor.Handles.DrawDottedLine(transform.position, condition.target.transform.position, 3f);

                // Метка состояния у цели
                string label = condition.IsSatisfied ? "✓" : $"нужно: {condition.requiredState}";
                GUIStyle style = new GUIStyle
                {
                    normal = { textColor = lineColor },
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                UnityEditor.Handles.Label(
                    condition.target.transform.position + Vector3.up * 0.8f,
                    label, style);
            }
            else
            {
                Gizmos.color = lineColor;
                Gizmos.DrawLine(transform.position, condition.target.transform.position);
            }
        }

        // Подпись
        if (UnityEditor.Selection.activeGameObject == gameObject)
        {
            GUIStyle nameStyle = new GUIStyle
            {
                normal = { textColor = puzzleColor },
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.7f,
                $"🧩 {puzzleName} ({_satisfiedConditions}/{conditions.Count})",
                nameStyle);
        }
    }
#endif
}

/// <summary>
/// Одно условие головоломки: объект должен быть в определённом состоянии.
/// </summary>
[System.Serializable]
public class CollapseCondition
{
    [Tooltip("Объект, состояние которого проверяется")]
    public Collapsible target;

    [Tooltip("Требуемое состояние")]
    public CollapseState requiredState;

    [Tooltip("Описание для дизайнера")]
    public string note;

    /// <summary>Выполнено ли условие</summary>
    public bool IsSatisfied => target != null && target.CurrentState == requiredState;
}