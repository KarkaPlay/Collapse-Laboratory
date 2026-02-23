using UnityEngine;
using UnityEngine.Events;

public class Switch : Interactable
{
    public bool isOn = false;

    public UnityEvent<bool> onSwitchStateChanged;
    public UnityEvent onSwitchOn;
    public UnityEvent onSwitchOff;

    public void MakeSwitch()
    {
        Debug.Log($"Переключили isOn с {isOn} на {!isOn}", gameObject);
        isOn = !isOn;

        InvokeEvents();
    }

    public void MakeSwitch(bool switchTo)
    {
        Debug.Log($"Переключили isOn с {isOn} на {!isOn}", gameObject);
        isOn = switchTo;

        InvokeEvents();
    }

    private void InvokeEvents()
    {
        onSwitchStateChanged.Invoke(isOn);

        if (isOn)
        {
            onSwitchOn.Invoke();
        }
        else
        {
            onSwitchOff.Invoke();
        }
    }
}
