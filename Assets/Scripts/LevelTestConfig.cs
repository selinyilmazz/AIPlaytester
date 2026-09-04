using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelTestConfig
{
    public string levelName;
    public GameObject levelRoot;
    public List<KeyInteraction> keysToReset;
}