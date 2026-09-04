using System;
using System.Collections.Generic;

[Serializable]
public class MultiProfileSummaryReport
{
    public string Timestamp;
    public List<LevelProfileSummary> Levels = new List<LevelProfileSummary>();
}
