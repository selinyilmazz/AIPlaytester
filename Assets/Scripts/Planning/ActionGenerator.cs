using System.Collections.Generic;
using UnityEngine;

public class ActionGenerator
{
    public List<ActionDefinition> GenerateActions()
    {
        List<ActionDefinition> actions = new List<ActionDefinition>();
        MonoBehaviour[] allObjects = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        foreach (MonoBehaviour obj in allObjects)
        {
            if (obj is IPlannable plannable)
            {
                actions.Add(plannable.GetActionDefinition());
            }
        }

        return actions;
    }
}
