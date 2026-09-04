using System.Collections.Generic;

public class LevelModelToActionDefinitions
{
    public List<ActionDefinition> Translate(GeneratedLevel level)
    {
        List<ActionDefinition> actions = new List<ActionDefinition>();

        foreach (LevelObjectDefinition obj in level.Objects)
        {
            actions.Add(TranslateObject(obj));
        }

        return actions;
    }

    private ActionDefinition TranslateObject(LevelObjectDefinition obj)
    {
        switch (obj.ObjectType)
        {
            case LevelObjectType.Key:
                return TranslateKey(obj);
            case LevelObjectType.Door:
                return TranslateDoor(obj);
            case LevelObjectType.Trap:
                return TranslateTrap(obj);
            case LevelObjectType.Decoy:
                return TranslateDecoy(obj);
            default:
                return TranslateDecoy(obj);
        }
    }

    private ActionDefinition TranslateKey(LevelObjectDefinition obj)
    {
        ActionDefinition action = new ActionDefinition();
        action.ActionId = obj.ObjectId;
        action.Effects.Add(new FactEffect(obj.ObjectId + "_Collected", true));
        action.Outcome = ActionOutcome.None;
        return action;
    }

    private ActionDefinition TranslateDoor(LevelObjectDefinition obj)
    {
        ActionDefinition action = new ActionDefinition();
        action.ActionId = obj.ObjectId;

        foreach (string relatedId in obj.RelatedObjectIds)
        {
            action.Preconditions.Add(relatedId + "_Collected");
        }

        action.Outcome = ActionOutcome.Win;
        return action;
    }

    private ActionDefinition TranslateTrap(LevelObjectDefinition obj)
    {
        ActionDefinition action = new ActionDefinition();
        action.ActionId = obj.ObjectId;
        action.Outcome = ActionOutcome.Fail;
        return action;
    }

    private ActionDefinition TranslateDecoy(LevelObjectDefinition obj)
    {
        ActionDefinition action = new ActionDefinition();
        action.ActionId = obj.ObjectId;
        action.Outcome = ActionOutcome.None;
        return action;
    }
}
