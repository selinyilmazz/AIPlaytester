using System.Collections.Generic;

public class TestResult
{
    public bool Success;
    public int Steps;
    public float TimeTaken;
    public int WrongMoves;

    public TestOutcome Outcome;
    public List<ActionTraceEntry> Trace = new List<ActionTraceEntry>();
}
