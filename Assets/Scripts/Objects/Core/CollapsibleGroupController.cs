using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CollapsibleGroupController : MonoBehaviour
{
    [Header("Динамичские схлопывающиеся объекты")]
    [SerializeField] private List<Collapsible> collapsibles = new();

    public List<Collapsible> Collapsibles { get => collapsibles; }

    [Header("Интервал между схлопываниями")]
    [SerializeField] public float switchStateInterval;

    private Coroutine dynamicStateSwitchingCoroutine;

    private float timeToDissolve;

    void Start()
    {
        if (collapsibles.Count == 0)
        {
            GameDebug.LogWarning($"{gameObject.name}: CollapseGroup содержит 0 элементов");
        }
        else
        {
            timeToDissolve = collapsibles[0].stateNew.GetComponent<Dissolvable>().timeToDissolve;
        }
    }

    public void StartDynamicStateSwitching()
    {
        if (dynamicStateSwitchingCoroutine != null)
        {
            GameDebug.LogWarning("DynamicStateSwitchingCoroutine уже запущен! Запускаем заново");
            StopCoroutine(dynamicStateSwitchingCoroutine);
            dynamicStateSwitchingCoroutine = null;
        }
        
        dynamicStateSwitchingCoroutine = StartCoroutine(DynamicStateSwitching());
    }

    public void StopDynamicStateSwitching()
    {
        if (dynamicStateSwitchingCoroutine != null)
        {
            StopCoroutine(dynamicStateSwitchingCoroutine);
            dynamicStateSwitchingCoroutine = null;
        }
        else
        {
            GameDebug.LogWarning("Попытка остановить DynamicStateSwitchingCoroutine, но он не запущен!");
        }
    }

    private IEnumerator DynamicStateSwitching()
    {
        // Если перестанет работать нестабильное схлопывание - раскомментировать
        //yield return new WaitForSeconds(switchStateInterval);

        while (true)
        {
            // Отключаем возможность взаимодействия со всеми объектами сразу
            foreach (var collapsible in collapsibles.Where(c => c.isDynamic))
            {
                collapsible.SetCanPlayerCollapse(false);
                collapsible.stateNew.OnCollapseUnhighlight();
                collapsible.stateOld.OnCollapseUnhighlight();
            }

            // Запускаем анимацию схлопывания для всех объектов одновременно
            var collapseCoroutines = collapsibles
                .Where(c => c.isDynamic)
                .Select(c => StartCoroutine(AnimateCollapse(c)))
                .ToList();
           

            // Ждем завершения анимации у всех объектов
            foreach (var coroutine in collapseCoroutines)
            {
                yield return coroutine;
            }

            // Ждем интервал перед следующим циклом
            yield return new WaitForSeconds(switchStateInterval);
        }
    }

    // Корутина для анимации схлопывания одного объекта
    private IEnumerator AnimateCollapse(Collapsible collapsible)
    {
        // Ждем время анимации
        yield return new WaitForSeconds(timeToDissolve * 2 + 0.1f);

        // Выполняем схлопывание
        collapsible.Collapse();

        // Включаем возможность взаимодействия обратно (если нужно)
        collapsible.SetCanPlayerCollapse(true);
    }

    public void SetCollapsiblesFromChildren()
    {
        collapsibles = GetComponentsInChildren<Collapsible>().ToList();
    }
}
