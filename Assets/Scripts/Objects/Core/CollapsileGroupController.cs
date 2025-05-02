using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CollapsileGroupController : MonoBehaviour
{
    [Header("Динамичские схлопывающиеся объекты")]
    [SerializeField] private List<Collapsible> collapsibles = new();

    public List<Collapsible> Collapsibles { get => collapsibles; }

    [Header("Интервал между схлопываниями")]
    [SerializeField] public float switchStateInterval;

    private Coroutine dynamicStateSwitchingCoroutine;

    void Start()
    {
        if (collapsibles.Count == 0)
        {
            GameDebug.LogWarning($"{gameObject.name}: CollapseGroup содержит 0 элементов");
        }
    }

    public void StartDynamicStateSwitching()
    {
        if (dynamicStateSwitchingCoroutine != null)
        {
            GameDebug.LogWarning("DynamicStateSwitchingCoroutine уже запущен! Запускаем заново");
            StopCoroutine(dynamicStateSwitchingCoroutine);
            dynamicStateSwitchingCoroutine = null;
            dynamicStateSwitchingCoroutine = StartCoroutine(DynamicStateSwitching());
        }
        else
        {
            dynamicStateSwitchingCoroutine = StartCoroutine(DynamicStateSwitching());
        }
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
        yield return new WaitForSeconds(switchStateInterval);

        while (true)
        {
            foreach (var collapsible in collapsibles)
            {
                Debug.Log($"{collapsible.name}. Динамический: {collapsible.isDynamic}");
                if (collapsible.isDynamic)
                {
                    collapsible.Collapse();
                }
            }
            yield return new WaitForSeconds(switchStateInterval);
        }
    }

    public void SetCollapsiblesFromChildren()
    {
        collapsibles = GetComponentsInChildren<Collapsible>().ToList();
    }
}
