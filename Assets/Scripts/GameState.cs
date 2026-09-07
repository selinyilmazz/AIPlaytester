using System.Collections.Generic;

public enum LevelStatus
{
    InProgress,
    Completed,
    Failed
}

public class GameState
{
    public LevelStatus Status;
    public List<IInteractable> Interactables;

    // GENERIC FRAMEWORK - ADIM 9: opsiyonel, adapter'a ozel "state imzasi". Varsayilan
    // null - hicbir adapter doldurmazsa (bugunku PuzzleGameAdapter/LevelScanner yolu
    // dahil) PlayerSimBrain eski Interactables.Count/Status mantigina AYNEN duser.
    // Bir adapter kendi state'ini (sayac degeri, seviye, vb.) anlamli sekilde temsil
    // eden bir string uretmek isterse burayi doldurur - CounterGame'e ozel degil,
    // herhangi bir adapter kullanabilir.
    public string StateSignature;

    // GENERIC FRAMEWORK - ADIM 11: opsiyonel, adapter'a ozel "hedefe uzaklik" sinyali.
    // null = adapter progress bilgisi saglamiyor (bugunku PuzzleGameAdapter/LevelScanner
    // yolu dahil) - PlayerSimBrain bu durumda ProgressClassification.Unknown uretir,
    // hicbir mevcut davranis (wasEffective/wasSafe/StateSignature) etkilenmez.
    // Dusuk deger = hedefe daha yakin (adapter kendi olcegini/formulunu serbestce
    // secer, tek sabit kural bu yon). CounterGame'e ozel degil, herhangi bir adapter
    // kullanabilir.
    public float? DistanceToGoal;
}