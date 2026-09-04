using System.Collections.Generic;
using UnityEngine;

public class SearchEngine
{
    public List<string> FindSolutionBFS(List<ActionDefinition> actions, int maxDepth)
    {
        Debug.Log("SearchEngine: LevelDefinition kullanmiyor, " + actions.Count + " ActionDefinition ile BFS calisiyor.");

        Queue<SearchState> queue = new Queue<SearchState>();
        HashSet<string> visited = new HashSet<string>();

        SearchState start = new SearchState();
        queue.Enqueue(start);
        visited.Add(start.GetSignature());

        while (queue.Count > 0)
        {
            SearchState current = queue.Dequeue();

            if (current.IsWin())
            {
                return current.ActionsTaken;
            }

            if (current.ActionsTaken.Count >= maxDepth)
            {
                continue;
            }

            foreach (ActionDefinition action in actions)
            {
                if (!ArePreconditionsMet(current, action))
                {
                    continue;
                }

                SearchState next = Simulate(current, action);

                if (next.IsFail())
                {
                    continue;
                }

                string signature = next.GetSignature();
                if (visited.Contains(signature))
                {
                    continue;
                }
                visited.Add(signature);

                next.ActionsTaken.Add(action.ActionId);
                queue.Enqueue(next);
            }
        }

        return null;
    }

    private bool ArePreconditionsMet(SearchState state, ActionDefinition action)
    {
        foreach (string precondition in action.Preconditions)
        {
            if (!state.Facts.Contains(precondition))
            {
                return false;
            }
        }

        return true;
    }

    private SearchState Simulate(SearchState state, ActionDefinition action)
    {
        SearchState next = state.Clone();

        foreach (FactEffect effect in action.Effects)
        {
            if (effect.Value)
            {
                next.Facts.Add(effect.Fact);
            }
            else
            {
                next.Facts.Remove(effect.Fact);
            }
        }

        next.Outcome = action.Outcome;

        return next;
    }
}
