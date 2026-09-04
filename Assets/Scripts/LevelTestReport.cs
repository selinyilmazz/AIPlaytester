using System;
using System.Collections.Generic;

[Serializable]
public class LevelTestReport
{
    public string LevelName;
    public string Timestamp;
    public string PlayerProfile;

    public float SolverSuccessRate;
    public float SolverAverageSteps;

    public float PlayerSuccessRate;
    public float PlayerAverageSteps;
    public float PlayerAverageWrongMoves;

    public float DifficultyScore;
    public string Classification;

    public List<FailedRunTrace> FailedRunTraces = new List<FailedRunTrace>();
}
