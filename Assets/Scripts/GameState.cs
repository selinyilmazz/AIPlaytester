using System.Collections.Generic;

public enum LevelStatus
{
    InProgress,
    Completed,
    Failed
}

public class GameState
{
    public LevelStatus Status;
    public List<IInteractable> Interactables;
}