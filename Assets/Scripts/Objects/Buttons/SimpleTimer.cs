using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class SimpleTimer : MonoBehaviour
{
    public float activeTime = 5f;
    public float currentTime;

    public UnityEvent onTimerStart;
    public UnityEvent onTimerTick;
    public UnityEvent onTimerEnd;

    private Coroutine timerCoroutine;

    public void StartTimer()
    {
        if (timerCoroutine != null)
        {
            Debug.LogWarning($"Таймер на {activeTime} сек уже запущен. Дождитесь окончания. Осталось {currentTime} сек");
        }
        else
        {
            timerCoroutine = StartCoroutine(TimerCoroutine());
        }
    }

    private IEnumerator TimerCoroutine()
    {
        onTimerStart.Invoke();

        currentTime = activeTime;
        while (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            onTimerTick.Invoke();
            yield return null;
        }

        currentTime = 0;
        timerCoroutine = null;
        onTimerEnd.Invoke();
    }
}
