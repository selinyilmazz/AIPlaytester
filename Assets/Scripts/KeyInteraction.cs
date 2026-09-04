using UnityEngine;

public class KeyInteraction : MonoBehaviour, IInteractable, IResettable, IPlannable
{
    public bool IsCollected { get; private set; } = false;

    void OnMouseDown()
    {
        Interact();
    }

    public void Interact()
    {
        if (IsCollected)
        {
            return;
        }

        Debug.Log("Anahtar alindi!");
        IsCollected = true;
        gameObject.SetActive(false);
    }

    public void ResetState()
    {
        IsCollected = false;
        gameObject.SetActive(true);
    }

    public ActionDefinition GetActionDefinition()
    {
        ActionDefinition action = new ActionDefinition();
        action.ActionId = gameObject.name;
        action.Effects.Add(new FactEffect(gameObject.name + "_Collected", true));
        action.Outcome = ActionOutcome.None;
        return action;
    }
}