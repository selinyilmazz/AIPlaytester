using System;

[Serializable]
public class ActionTraceEntry
{
    public int StepIndex;
    public string ActionId;
    public bool WasEffective;
    public int InteractableCountBefore;
    public int InteractableCountAfter;
    public LevelStatus ResultingStatus;
}
