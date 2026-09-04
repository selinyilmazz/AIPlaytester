public class ActionCandidate
{
    public IInteractable Interactable;
    public string ActionId;

    public ActionCandidate(IInteractable interactable, string actionId)
    {
        Interactable = interactable;
        ActionId = actionId;
    }
}
