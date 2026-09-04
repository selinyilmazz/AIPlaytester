using System;
using System.Collections.Generic;

[Serializable]
public class FailedRunTrace
{
    public string RunLabel;
    public TestOutcome Outcome;
    public List<ActionTraceEntry> Trace;
}
