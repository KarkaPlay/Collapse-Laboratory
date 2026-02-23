using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class ConditionList : MonoBehaviour
{
    public List<Condition> conditions;

    public UnityEvent onAllConditionsTrue;

    public bool AreAllConditionsTrue()
    {
        foreach (var condition in conditions)
        {
            if (!condition.isTrue)
                return false;
        }

        Debug.Log("All conditions are true!", gameObject);
        return true;
    }

    public void SetConditionTrue(string conditionName)
    {
        GetConditionByName(conditionName).SetCondition(true);

        if (AreAllConditionsTrue())
        {
            onAllConditionsTrue.Invoke();
        }
    }

    public void SetConditionFalse(string conditionName)
    {
        GetConditionByName(conditionName).SetCondition(false);
    }

    private Condition GetConditionByName(string conditionName)
    {
        Condition foundCond = conditions.Find(condition => condition.conditionName == conditionName);

        if (foundCond == null)
        {
            Debug.LogWarning($"Condition with name '{conditionName}' not found.");
        }

        return foundCond;
    }
}
