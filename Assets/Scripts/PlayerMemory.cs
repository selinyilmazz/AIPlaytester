using System.Collections.Generic;

public class PlayerMemory
{
    private class ActionRecord
    {
        public int TriedCount;
        public bool LastAttemptWasEffective;
        public bool LastAttemptWasSafe;
    }

    private Dictionary<string, ActionRecord> records = new Dictionary<string, ActionRecord>();

    public bool HasBeenTried(string actionId)
    {
        return records.ContainsKey(actionId);
    }

    public int GetTriedCount(string actionId)
    {
        return records.TryGetValue(actionId, out ActionRecord record) ? record.TriedCount : 0;
    }

    public bool WasLastAttemptEffective(string actionId)
    {
        return records.TryGetValue(actionId, out ActionRecord record) && record.LastAttemptWasEffective;
    }

    public bool WasLastAttemptSafe(string actionId)
    {
        return !records.TryGetValue(actionId, out ActionRecord record) || record.LastAttemptWasSafe;
    }

    public void RecordAttempt(string actionId, bool wasEffective, bool wasSafe)
    {
        if (!records.TryGetValue(actionId, out ActionRecord record))
        {
            record = new ActionRecord();
            records[actionId] = record;
        }

        record.TriedCount++;
        record.LastAttemptWasEffective = wasEffective;
        record.LastAttemptWasSafe = wasSafe;
    }
}
