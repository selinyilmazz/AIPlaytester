using System.Collections.Generic;

public class SearchState
{
    public HashSet<string> Facts = new HashSet<string>();
    public List<string> ActionsTaken = new List<string>();
    public ActionOutcome Outcome = ActionOutcome.None;

    public SearchState Clone()
    {
        SearchState clone = new SearchState();
        clone.Facts = new HashSet<string>(Facts);
        clone.ActionsTaken = new List<string>(ActionsTaken);
        clone.Outcome = Outcome;
        return clone;
    }

    public bool IsWin()
    {
        return Outcome == ActionOutcome.Win;
    }

    public bool IsFail()
    {
        return Outcome == ActionOutcome.Fail;
    }

    public string GetSignature()
    {
        List<string> sortedFacts = new List<string>(Facts);
        sortedFacts.Sort();
        return string.Join("|", sortedFacts) + "::" + Outcome;
    }
}
