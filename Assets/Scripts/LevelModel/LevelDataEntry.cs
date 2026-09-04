using System;

[Serializable]
public class LevelDataEntry
{
    public string Key;
    public string Value;

    public LevelDataEntry(string key, string value)
    {
        Key = key;
        Value = value;
    }
}
