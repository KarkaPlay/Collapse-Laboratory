using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class CollapsibleGroupController : MonoBehaviour
{
    [Header("Динамичские схлопывающиеся объекты")]
    [SerializeField] private List<Collapsible> collapsibles = new();

    public List<Collapsible> Collapsibles => collapsibles;

    [Header("Интервал между схлопываниями")]
    [SerializeField] public float switchStateInterval;

    private Coroutine _dynamicStateSwitchingCoroutine;

    private float _timeToDissolve;
    
    public float timeSinceLastSwitch { get; private set; }

    private void Start()
    {
        if (collapsibles.Count == 0)
        {
            GameDebug.LogWarning($"{gameObject.name}: CollapseGroup содержит 0 элементов");
        }
        else
        {
            _timeToDissolve = collapsibles[0].stateNew.GetComponent<Dissolvable>().timeToDissolve;
        }
    }

    public void StartDynamicStateSwitching()
    {
        if (_dynamicStateSwitchingCoroutine != null)
        {
            GameDebug.LogWarning("DynamicStateSwitchingCoroutine уже запущен! Запускаем заново");
            StopCoroutine(_dynamicStateSwitchingCoroutine);
            _dynamicStateSwitchingCoroutine = null;
        }
        
        _dynamicStateSwitchingCoroutine = StartCoroutine(DynamicStateSwitching());
    }

    public void StopDynamicStateSwitching()
    {
        if (_dynamicStateSwitchingCoroutine != null)
        {
            StopCoroutine(_dynamicStateSwitchingCoroutine);
            _dynamicStateSwitchingCoroutine = null;
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
        
        // TODO: Где-то тут нужно будет сделать, чтобы таймер ждал, пока закончится анимация диссолва у всех объектов

        while (true)
        {
            timeSinceLastSwitch = 0f;
            
            // Отключаем возможность взаимодействия со всеми объектами сразу
            foreach (var collapsible in collapsibles.Where(c => c.isDynamic))
            {
                //collapsible.SetCanPlayerCollapse(false);
                collapsible.stateNew.OnUnhighlight();
                collapsible.stateOld.OnUnhighlight();
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

            // Ждем интервал перед следующим циклом, используя дельта времени
            while (timeSinceLastSwitch < switchStateInterval)
            {
                yield return null; // Ждем следующий кадр
                timeSinceLastSwitch += Time.deltaTime;
            }
        }
    }

    // Корутина для анимации схлопывания одного объекта
    private IEnumerator AnimateCollapse(Collapsible collapsible)
    {
        // Ждем время анимации
        yield return new WaitForSeconds(_timeToDissolve * 2 + 0.1f);

        // Выполняем схлопывание
        collapsible.Collapse();

        // Включаем возможность взаимодействия обратно (если нужно)
        //collapsible.SetCanPlayerCollapse(true);
    }

    #region Editor

    public void SetCollapsiblesFromChildren()
    {
        collapsibles = GetComponentsInChildren<Collapsible>().ToList();
    }
    
    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (collapsibles == null || collapsibles.Count < 2) return;

        var position = transform.position;
        bool isSelected = Selection.activeGameObject == gameObject;
        
        // Draw lines between all collapsibles in the group
        for (int i = 0; i < collapsibles.Count; i++)
        {
            if (collapsibles[i] == null) continue;
            
            var startPos = collapsibles[i].transform.position;
            
            for (int j = i + 1; j < collapsibles.Count; j++)
            {
                if (collapsibles[j] == null) continue;
                
                var endPos = collapsibles[j].transform.position;
                
                if (isSelected)
                {
                    // Use Handles for selected object (thicker line, draws on top)
                    Handles.color = new Color(1f, 0.7f, 0f, 0.8f); // Orange color for group
                    Handles.DrawLine(startPos, endPos, 3f);
                }
                else
                {
                    // Use Gizmos for unselected objects
                    Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // Orange color for group
                    Gizmos.DrawLine(startPos, endPos);
                }
            }
            
            // Draw a small wire sphere at each collapsible position
            if (isSelected)
            {
                Handles.color = new Color(1f, 0.7f, 0f, 0.8f);
                Handles.SphereHandleCap(0, startPos, Quaternion.identity, 0.2f, EventType.Repaint);
            }
            else
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
                Gizmos.DrawWireSphere(startPos, 0.2f);
            }
        }
#endif
    }

    #endregion
}
