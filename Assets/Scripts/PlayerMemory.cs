using System.Collections.Generic;

// GENERIC FRAMEWORK - ADIM 11: bir aksiyonun, mevcut state'e gore hedefe DOGRU mu
// (Productive) yoksa hedeften UZAGA mi (Regressive) hareket ettirdigini, yoksa
// bunun bilinmedigini (Unknown - adapter DistanceToGoal saglamiyorsa) ya da hicbir
// degisiklik olmadigini (Neutral) temsil eder. Unknown ILK eleman - bu, C#'ta bir
// enum alaninin varsayilan (atanmamis) degerinin otomatik olarak Unknown olmasini
// saglar, ayrica bir baslangic degeri yazmaya gerek kalmaz.
public enum ProgressClassification
{
    Unknown,
    Productive,
    Neutral,
    Regressive
}

public class PlayerMemory
{
    private class ActionRecord
    {
        public int TriedCount;
        public bool LastAttemptWasEffective;
        public bool LastAttemptWasSafe;

        // GENERIC FRAMEWORK - ADIM 11: varsayilan deger enum'un ilk elemani olan
        // Unknown - hicbir ek atama gerekmez.
        public ProgressClassification LastAttemptProgress;
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

    // GENERIC FRAMEWORK - ADIM 11: bu actionId hic denenmemisse ya da denenmis ama
    // progress bilgisi hic saglanmamissa (adapter DistanceToGoal doldurmuyorsa)
    // Unknown doner - HasBeenTried/WasLastAttemptEffective/WasLastAttemptSafe'in
    // davranisina hicbir etkisi yok, tamamen bagimsiz, ek bir okuma noktasi.
    public ProgressClassification GetLastAttemptProgress(string actionId)
    {
        return records.TryGetValue(actionId, out ActionRecord record) ? record.LastAttemptProgress : ProgressClassification.Unknown;
    }

    // GENERIC FRAMEWORK - ADIM 11: 'progress' OPSIYONEL bir parametre, varsayilani
    // Unknown. Bu sayede mevcut cagri sekli RecordAttempt(actionId, wasEffective,
    // wasSafe) DEGISMEDEN derlenmeye devam eder - wasEffective/wasSafe'in yazildigi
    // satirlar da AYNEN korunuyor, sadece yanina progress eklendi.
    public void RecordAttempt(string actionId, bool wasEffective, bool wasSafe, ProgressClassification progress = ProgressClassification.Unknown)
    {
        if (!records.TryGetValue(actionId, out ActionRecord record))
        {
            record = new ActionRecord();
            records[actionId] = record;
        }

        record.TriedCount++;
        record.LastAttemptWasEffective = wasEffective;
        record.LastAttemptWasSafe = wasSafe;
        record.LastAttemptProgress = progress;
    }
}
