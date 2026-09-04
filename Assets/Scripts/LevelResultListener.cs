using UnityEngine;

public class LevelResultListener : MonoBehaviour
{
    void Start()
    {
        LevelManager.Instance.OnLevelCompleted += HandleLevelCompleted;
        LevelManager.Instance.OnLevelFailed += HandleLevelFailed;
    }

    void HandleLevelCompleted()
    {
        Debug.Log("Dinleyici: Tebrikler, leveli kazandin!");
    }

    void HandleLevelFailed()
    {
        Debug.Log("Dinleyici: Level basarisiz oldu.");
    }
}