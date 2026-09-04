using System.Collections.Generic;
using UnityEngine;

// ---------------------------------------------------------------------------
// SECOND GAME PROOF - CounterGameAdapter
//
// PuzzleGame'den TAMAMEN BAGIMSIZ, ikinci bir oyun turunu mevcut IGameAdapter
// mimarisi uzerinden framework'e baglayan kanit niteliginde bir adapter.
// Hicbir mevcut dosyaya (MultiLevelTestRunner.cs, PlayerSimBrain.cs,
// PuzzleGameAdapter.cs, IGameAdapter.cs, IGameLevelProvider.cs, GameState.cs,
// ActionCandidate.cs, dashboard) DOKUNULMADI - bu sinif SADECE zaten var olan
// arayuzleri implement ediyor.
//
// Oyun kurallari:
//   - Sayac (currentValue) 0'dan baslar.
//   - Butonlar (CounterButtonInteraction) sabit bir delta uygular (+1/+5/-1 gibi).
//   - Sayac aktif levelin "target" degerine ESIT olursa -> Completed.
//   - Sayac aktif levelin "failThreshold" degerine ULASIR/ASARSA -> Failed.
//   - Diger her durumda -> InProgress.
//
// SupportsSolver neden false: bu oyunun durumu surekli artip azalabilen,
// tersinir bir sayi - Key/Door'un tek yonlu (collect-then-fixed) boolean
// fact modeline dogal olarak uymuyor. Mevcut ActionGenerator/SearchEngine
// (fact/precondition/effect tabanli BFS) bu tur icin anlamli bir sonuc
// uretemez. false donerek MultiLevelTestRunner'in solver gecisini bu level
// icin otomatik atlamasini sagliyoruz (bkz. IGameAdapter.SupportsSolver).
// ---------------------------------------------------------------------------

public class CounterGameAdapter : MonoBehaviour, IGameAdapter, IGameLevelProvider, IGameStateReader, IGameActionExecutor
{
    // CounterGame'e ozel, kucuk ve tamamen bu adapter'a ait bir level tanimi.
    // LevelTestConfig'e (levelRoot/keysToReset - puzzle'a ozel) KASITLI OLARAK
    // bagli degil; bu oyunun leveli sadece bir isim + iki sayidan olusuyor.
    [System.Serializable]
    public class CounterLevelDefinition
    {
        public string levelName;
        public int target;
        public int failThreshold;
    }

    [Header("Bu oyunun kendi, PuzzleGame'den bagimsiz level tanimlari")]
    public List<CounterLevelDefinition> levels = new List<CounterLevelDefinition>();

    [Header("Sahnedeki butonlar - dogrudan Inspector referansi, sahne taramasi yok")]
    public List<CounterButtonInteraction> buttons = new List<CounterButtonInteraction>();

    private CounterLevelDefinition activeLevel;
    private int currentValue;

    // IGameAdapter: alt sozlesmeleri disari acar (PuzzleGameAdapter ile ayni kalip).
    public IGameLevelProvider Levels => this;
    public IGameStateReader StateReader => this;
    public IGameActionExecutor ActionExecutor => this;

    // -----------------------------------------------------------------
    // IGameLevelProvider
    // -----------------------------------------------------------------

    public IEnumerable<string> GetLevels()
    {
        List<string> names = new List<string>();
        foreach (CounterLevelDefinition level in levels)
        {
            names.Add(level.levelName);
        }
        return names;
    }

    public void LoadLevel(string levelId)
    {
        CounterLevelDefinition match = FindLevelDefinition(levelId);
        if (match == null)
        {
            Debug.LogWarning("CounterGameAdapter: '" + levelId + "' adinda bir level bulunamadi, LoadLevel hicbir sey yapmadi.");
            return;
        }

        activeLevel = match;
        currentValue = 0;
    }

    public void ResetLevel(string levelId)
    {
        CounterLevelDefinition match = FindLevelDefinition(levelId);
        if (match == null)
        {
            Debug.LogWarning("CounterGameAdapter: '" + levelId + "' adinda bir level bulunamadi.");
            return;
        }

        // ResetLevel(): sayaci 0'a doner VE aktif target/failThreshold
        // degerlerini bu levele gore (yeniden) secer - LoadLevel ile ayni
        // islemi yapar, cunku bu oyunda "yukleme" ve "resetleme" arasinda
        // (GameObject aktiflestirme/deaktiflestirme olmadigi icin) fark yok.
        activeLevel = match;
        currentValue = 0;
    }

    private CounterLevelDefinition FindLevelDefinition(string levelId)
    {
        foreach (CounterLevelDefinition level in levels)
        {
            if (level.levelName == levelId)
            {
                return level;
            }
        }

        return null;
    }

    // -----------------------------------------------------------------
    // IGameStateReader
    // -----------------------------------------------------------------

    public GameState GetGameState()
    {
        GameState state = new GameState();
        state.Status = ComputeStatus();
        state.Interactables = new List<IInteractable>();

        foreach (CounterButtonInteraction button in buttons)
        {
            if (button != null)
            {
                state.Interactables.Add(button);
            }
        }

        return state;
    }

    private LevelStatus ComputeStatus()
    {
        if (activeLevel == null)
        {
            return LevelStatus.InProgress;
        }

        // Once "kazanma" kontrolu: sayac tam olarak hedefe esitse Completed.
        if (currentValue == activeLevel.target)
        {
            return LevelStatus.Completed;
        }

        // Sonra "kaybetme" kontrolu: sayac esigi ulasir/asarsa Failed.
        if (currentValue >= activeLevel.failThreshold)
        {
            return LevelStatus.Failed;
        }

        return LevelStatus.InProgress;
    }

    // -----------------------------------------------------------------
    // IGameActionExecutor
    // -----------------------------------------------------------------

    public List<ActionCandidate> GetAvailableActions()
    {
        List<ActionCandidate> candidates = new List<ActionCandidate>();

        foreach (CounterButtonInteraction button in buttons)
        {
            if (button != null)
            {
                candidates.Add(new ActionCandidate(button, button.gameObject.name));
            }
        }

        return candidates;
    }

    public void Execute(ActionCandidate action)
    {
        action.Interactable.Interact();
    }

    // -----------------------------------------------------------------
    // IGameAdapter - opsiyonel ek metrik noktasi
    // PuzzleGameAdapter ile ayni: bu oyun icin de ozel bir metrik yok, bos
    // dictionary donuluyor. Core rapor semasina hicbir etkisi yok.
    // -----------------------------------------------------------------

    public Dictionary<string, float> GetCustomMetrics()
    {
        return new Dictionary<string, float>();
    }

    public bool SupportsSolver => false;

    // -----------------------------------------------------------------
    // CounterButtonInteraction tarafindan cagrilir - sayac state'i SADECE
    // burada degistirilir (tek yazma noktasi).
    // -----------------------------------------------------------------

    public void ApplyDelta(int delta)
    {
        currentValue += delta;
    }
}
