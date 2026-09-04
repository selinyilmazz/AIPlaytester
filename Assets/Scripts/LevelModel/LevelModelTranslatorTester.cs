using System.Collections.Generic;
using UnityEngine;

public class LevelModelTranslatorTester : MonoBehaviour
{
    void Start()
    {
        RunTest(BuildLevel01());
        RunTest(BuildLevel02());
    }

    private void RunTest(GeneratedLevel level)
    {
        Debug.Log("===== " + level.LevelId + " =====");

        LevelModelToActionDefinitions translator = new LevelModelToActionDefinitions();
        List<ActionDefinition> actions = translator.Translate(level);

        foreach (ActionDefinition action in actions)
        {
            string preconditions = action.Preconditions.Count > 0 ? string.Join(", ", action.Preconditions) : "-";
            string effects = action.Effects.Count > 0 ? EffectsToString(action.Effects) : "-";

            Debug.Log(action.ActionId +
                " | Preconditions: " + preconditions +
                " | Effects: " + effects +
                " | Outcome: " + action.Outcome);
        }

        SearchEngine engine = new SearchEngine();
        List<string> solution = engine.FindSolutionBFS(actions, 10);

        if (solution == null)
        {
            Debug.Log(level.LevelId + ": cozum bulunamadi.");
        }
        else
        {
            Debug.Log(level.LevelId + " cozumu -> " + string.Join(" -> ", solution));
        }
    }

    private string EffectsToString(List<FactEffect> effects)
    {
        List<string> parts = new List<string>();
        foreach (FactEffect effect in effects)
        {
            parts.Add(effect.Fact + " = " + effect.Value);
        }
        return string.Join(", ", parts);
    }

    private GeneratedLevel BuildLevel01()
    {
        GeneratedLevel level = new GeneratedLevel();
        level.LevelId = "Level01_Manual";
        level.SequenceIndex = 1;
        level.Seed = 0;
        level.GeneratorVersion = "manual-0.0";

        level.Objects.Add(CreateObject("Key", LevelObjectType.Key, null));
        level.Objects.Add(CreateObject("Door", LevelObjectType.Door, new List<string> { "Key" }));
        level.Objects.Add(CreateObject("Trap", LevelObjectType.Trap, null));
        level.Objects.Add(CreateObject("Button", LevelObjectType.Decoy, null));

        return level;
    }

    private GeneratedLevel BuildLevel02()
    {
        GeneratedLevel level = new GeneratedLevel();
        level.LevelId = "Level02_Manual";
        level.SequenceIndex = 2;
        level.Seed = 0;
        level.GeneratorVersion = "manual-0.0";

        level.Objects.Add(CreateObject("Key2A", LevelObjectType.Key, null));
        level.Objects.Add(CreateObject("Key2B", LevelObjectType.Key, null));
        level.Objects.Add(CreateObject("Door2", LevelObjectType.Door, new List<string> { "Key2A", "Key2B" }));
        level.Objects.Add(CreateObject("Trap2", LevelObjectType.Trap, null));
        level.Objects.Add(CreateObject("Button2", LevelObjectType.Decoy, null));

        return level;
    }

    private LevelObjectDefinition CreateObject(string objectId, LevelObjectType type, List<string> relatedIds)
    {
        LevelObjectDefinition obj = new LevelObjectDefinition();
        obj.ObjectId = objectId;
        obj.ObjectType = type;
        obj.Position = new PositionData(0f, 0f, 0f);

        if (relatedIds != null)
        {
            obj.RelatedObjectIds = relatedIds;
        }

        return obj;
    }
}
