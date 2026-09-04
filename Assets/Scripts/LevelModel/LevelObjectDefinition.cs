using System;
using System.Collections.Generic;

[Serializable]
public class LevelObjectDefinition
{
    public string ObjectId;
    public LevelObjectType ObjectType;
    public PositionData Position;
    public List<string> RelatedObjectIds = new List<string>();
    public List<LevelDataEntry> Parameters = new List<LevelDataEntry>();
}
