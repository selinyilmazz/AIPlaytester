using System;
using System.Collections.Generic;

[Serializable]
public class GeneratedLevel
{
    public string LevelId;
    public int SequenceIndex;
    public long Seed;
    public string GeneratorVersion;

    public List<LevelObjectDefinition> Objects = new List<LevelObjectDefinition>();
    public List<LevelDataEntry> Metadata = new List<LevelDataEntry>();
}
