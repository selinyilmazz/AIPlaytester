using System.Collections.Generic;

public interface IPlayerProfile
{
    ActionCandidate ChooseAction(List<ActionCandidate> candidates, PlayerMemory memory);
    float GetThinkingDelay();
}
