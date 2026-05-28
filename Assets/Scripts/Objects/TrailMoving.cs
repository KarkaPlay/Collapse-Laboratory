using CollapseSettings;
using System.Collections;
using UnityEngine;

/// <summary>
/// Создаёт визуальный эффект "следа" от одного объекта к другому.
/// Используется для визуализации связей между Collapsible объектами.
/// Автоматически получает prefab из CollapseLabSettings если не назначен вручную.
/// </summary>
public class TrailMoving : MonoBehaviour
{
    [Header("Эффект связи между объектами")]
    [Tooltip("Префаб визуального эффекта (если не назначен, берётся из CollapseLabSettings)")]
    public GameObject trailPrefab;

    [Header("Настройки движения")]
    [Tooltip("Скорость движения эффекта (используется если timeToMove = 0)")]
    public float speed = 5f;

    private float _timeToMove;
    private bool _settingsChecked = false;

    /// <summary>
    /// Установить время движения для следующего trail.
    /// </summary>
    public void SetTimeToMove(float newTime)
    {
        _timeToMove = newTime;
    }

    /// <summary>
    /// Запустить визуальный след от this.transform к target.
    /// </summary>
    public void StartTrail(Transform target)
    {
        // Получаем prefab (из поля или из настроек)
        GameObject prefabToUse = GetTrailPrefab();

        if (prefabToUse == null)
        {
            GameDebug.LogWarning($"[TrailMoving] {name}: trailPrefab не назначен и не найден в CollapseLabSettings!");
            return;
        }

        if (target == null)
        {
            GameDebug.LogWarning($"[TrailMoving] {name}: target is null!");
            return;
        }

        // Создаём экземпляр эффекта
        GameObject trail = Instantiate(prefabToUse, transform.position, Quaternion.identity);

        // Запускаем корутину движения
        StartCoroutine(MoveTrailCoroutine(trail.transform, target));
    }

    /// <summary>
    /// Получает trail prefab: сначала проверяет локальное поле, потом настройки.
    /// </summary>
    private GameObject GetTrailPrefab()
    {
        // Если назначен вручную — используем его
        if (trailPrefab != null)
        {
            return trailPrefab;
        }

        // Иначе пытаемся получить из настроек (только один раз)
        if (!_settingsChecked)
        {
            _settingsChecked = true;

            var settings = CollapseLabSettings.Instance;
            if (settings != null && settings.trailPrefab != null)
            {
                trailPrefab = settings.trailPrefab;
                GameDebug.Log($"[TrailMoving] {name}: Trail prefab загружен из CollapseLabSettings");
            }
            else if (settings == null)
            {
                GameDebug.LogWarning("[TrailMoving] CollapseLabSettings не найден в Resources!");
            }
        }

        return trailPrefab;
    }

    private IEnumerator MoveTrailCoroutine(Transform trailTransform, Transform target)
    {
        if (trailTransform == null || target == null)
        {
            yield break;
        }

        Vector3 startPosition = transform.position;
        Vector3 endPosition = target.position;

        float elapsed = 0f;
        float duration = _timeToMove > 0 ? _timeToMove : Vector3.Distance(startPosition, endPosition) / speed;

        // Защита от деления на ноль
        if (duration <= 0)
        {
            duration = 0.5f;
        }

        // Двигаем trail от источника к цели
        while (elapsed < duration)
        {
            if (trailTransform == null) yield break;

            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Lerp с обновлением конечной позиции (если цель движется)
            Vector3 currentEnd = target != null ? target.position : endPosition;
            trailTransform.position = Vector3.Lerp(startPosition, currentEnd, t);

            // Поворачиваем trail в направлении движения
            Vector3 direction = currentEnd - trailTransform.position;
            if (direction != Vector3.zero)
            {
                trailTransform.rotation = Quaternion.LookRotation(direction);
            }

            yield return null;
        }

        // Финальная позиция
        if (trailTransform != null && target != null)
        {
            trailTransform.position = target.position;
        }

        // Эффект уничтожится сам через AutoDestroy или Particle System duration
    }

    // Для визуализации в редакторе
    private void OnDrawGizmosSelected()
    {
        GameObject prefab = GetTrailPrefab();

        if (prefab != null)
        {
            Gizmos.color = new Color(0.5f, 1f, 0.5f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, 0.3f);

#if UNITY_EDITOR
            // Показываем откуда взят prefab
            string source = trailPrefab != null ? "Local" : "Settings";
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.5f,
                $"Trail: {prefab.name} ({source})",
                new GUIStyle
                {
                    normal = { textColor = new Color(0.5f, 1f, 0.5f) },
                    fontSize = 10,
                    alignment = TextAnchor.MiddleCenter
                });
#endif
        }
        else
        {
            // Предупреждение если prefab не найден
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, 0.3f);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.5f,
                "⚠ Trail Prefab не назначен!",
                new GUIStyle
                {
                    normal = { textColor = new Color(1f, 0.5f, 0f) },
                    fontSize = 10,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                });
#endif
        }
    }
}