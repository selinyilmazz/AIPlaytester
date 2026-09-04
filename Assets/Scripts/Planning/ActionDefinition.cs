using System.Collections.Generic;

public class ActionDefinition
{
    public string ActionId;
    public List<string> Preconditions = new List<string>();
    public List<FactEffect> Effects = new List<FactEffect>();
    public ActionOutcome Outcome = ActionOutcome.None;
}
