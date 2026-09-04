using System;
using System.Collections.Generic;

[Serializable]
public class LevelProfileSummary
{
    public string LevelName;
    public List<ProfileSummaryEntry> Profiles = new List<ProfileSummaryEntry>();
}
