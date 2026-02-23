using UnityEngine.Events;

[System.Serializable]
public class Condition
{
    public string conditionName;
    public string description;
    public bool isTrue;

    public UnityEvent onConditionTrue;
    public UnityEvent onConditionFalse;
    public UnityEvent<bool> onConditionStateChange;

    public void SetCondition(bool newState)
    {
        this.isTrue = newState;
        onConditionStateChange.Invoke(newState);
        if (isTrue)
        {
            onConditionTrue.Invoke();
        }
        else
        {
            onConditionFalse.Invoke();
        }
    }
}
