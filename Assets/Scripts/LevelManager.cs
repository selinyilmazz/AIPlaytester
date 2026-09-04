using System;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public event Action OnLevelCompleted;
    public event Action OnLevelFailed;

    public LevelStatus CurrentStatus { get; private set; } = LevelStatus.InProgress;

    void Awake()
    {
        Instance = this;
    }

    public void CompleteLevel()
    {
        CurrentStatus = LevelStatus.Completed;
        Debug.Log("LevelManager: Level tamamlandi sinyali gonderiliyor.");
        OnLevelCompleted?.Invoke();
    }

    public void FailLevel()
    {
        CurrentStatus = LevelStatus.Failed;
        Debug.Log("LevelManager: Level basarisiz sinyali gonderiliyor.");
        OnLevelFailed?.Invoke();
    }

    public void ResetLevel()
    {
        CurrentStatus = LevelStatus.InProgress;
        Debug.Log("LevelManager: Level resetlendi.");
    }
}