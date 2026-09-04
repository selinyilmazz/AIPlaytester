using System.Collections.Generic;
using UnityEngine;

public class LevelScanner : MonoBehaviour
{
    void Start()
    {
        GameState initialState = GetCurrentState();
        Debug.Log("Baslangic durumu -> Status: " + initialState.Status + ", Interactable sayisi: " + initialState.Interactables.Count);

        LevelManager.Instance.OnLevelCompleted += LogCurrentState;
        LevelManager.Instance.OnLevelFailed += LogCurrentState;
    }

    void LogCurrentState()
    {
        GameState state = GetCurrentState();
        Debug.Log("Guncel durum -> Status: " + state.Status);
    }

    public GameState GetCurrentState()
    {
        GameState state = new GameState();
        state.Status = LevelManager.Instance.CurrentStatus;
        state.Interactables = FindAllInteractables();
        return state;
    }

    public List<IInteractable> FindAllInteractables()
    {
        List<IInteractable> result = new List<IInteractable>();
        MonoBehaviour[] allObjects = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        foreach (MonoBehaviour obj in allObjects)
        {
            if (obj is IInteractable interactable)
            {
                result.Add(interactable);
            }
        }

        return result;
    }
}