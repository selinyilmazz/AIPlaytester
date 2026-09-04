using System.Collections.Generic;
using UnityEngine;

public class ImpulsivePlayerProfile : IPlayerProfile
{
    private const float UntriedWeight = 1.2f;
    private const float EffectiveWeight = 1f;
    private const float IneffectiveWeight = 0.7f;
    private const float UnsafeWeight = 0.25f;

    public ActionCandidate ChooseAction(List<ActionCandidate> candidates, PlayerMemory memory)
    {
        List<float> weights = new List<float>();
        float totalWeight = 0f;

        foreach (ActionCandidate candidate in candidates)
        {
            float weight = GetWeight(candidate.ActionId, memory);
            weights.Add(weight);
            totalWeight += weight;
        }

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < candidates.Count; i++)
        {
            cumulative += weights[i];
            if (roll <= cumulative)
            {
                return candidates[i];
            }
        }

        return candidates[candidates.Count - 1];
    }

    public float GetThinkingDelay()
    {
        return 0f;
    }

    private float GetWeight(string actionId, PlayerMemory memory)
    {
        if (!memory.HasBeenTried(actionId))
        {
            return UntriedWeight;
        }

        if (!memory.WasLastAttemptSafe(actionId))
        {
            return UnsafeWeight;
        }

        return memory.WasLastAttemptEffective(actionId) ? EffectiveWeight : IneffectiveWeight;
    }
}
