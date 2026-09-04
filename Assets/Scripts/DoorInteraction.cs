using System.Collections.Generic;
using UnityEngine;

public class DoorInteraction : MonoBehaviour, IInteractable, IPlannable
{
    public List<KeyInteraction> requiredKeys;

    void OnMouseDown()
    {
        Interact();
    }

    public void Interact()
    {
        if (!AllKeysCollected())
        {
            Debug.Log("Kapi kilitli, once tum anahtarlari almalisin!");
            return;
        }

        Debug.Log("Kapi acildi!");
        LevelManager.Instance.CompleteLevel();
    }

    private bool AllKeysCollected()
    {
        foreach (KeyInteraction key in requiredKeys)
        {
            if (key == null || !key.IsCollected)
            {
                return false;
            }
        }

        return true;
    }

    public ActionDefinition GetActionDefinition()
    {
        ActionDefinition action = new ActionDefinition();
        action.ActionId = gameObject.name;
        foreach (KeyInteraction key in requiredKeys)
        {
            if (key != null)
            {
                action.Preconditions.Add(key.gameObject.name + "_Collected");
            }
        }
        action.Outcome = ActionOutcome.Win;
        return action;
    }
}