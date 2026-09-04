using UnityEngine;

public class TrapInteraction : MonoBehaviour, IInteractable, IPlannable
{
    void OnMouseDown()
    {
        Interact();
    }

    public void Interact()
    {
        Debug.Log("Tuzaga tiklandi!");
        LevelManager.Instance.FailLevel();
    }

    public ActionDefinition GetActionDefinition()
    {
        ActionDefinition action = new ActionDefinition();
        action.ActionId = gameObject.name;
        action.Outcome = ActionOutcome.Fail;
        return action;
    }
}