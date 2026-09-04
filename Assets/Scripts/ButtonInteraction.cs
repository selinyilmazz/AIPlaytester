using UnityEngine;

public class ButtonInteraction : MonoBehaviour, IInteractable, IPlannable
{
    void OnMouseDown()
    {
        Interact();
    }

    public void Interact()
    {
        Debug.Log("Butona tiklandi, ama bu bir tuzak degil, sadece hicbir sonucu yok (decoy).");
    }

    public ActionDefinition GetActionDefinition()
    {
        ActionDefinition action = new ActionDefinition();
        action.ActionId = gameObject.name;
        action.Outcome = ActionOutcome.None;
        return action;
    }
}