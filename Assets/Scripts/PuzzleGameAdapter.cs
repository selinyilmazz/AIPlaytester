using System.Collections.Generic;
using UnityEngine;

// ---------------------------------------------------------------------------
// GENERIC FRAMEWORK - ADIM 2
//
// Mevcut escape-room tipi puzzle oyununu (LevelTestConfig tabanli level sistemi,
// LevelScanner tabanli state okuma, LevelManager tabanli win/lose, IInteractable
// tabanli aksiyon uygulama) IGameAdapter sozlesmesi arkasinda SARMALAYAN adapter.
//
// KURAL: Bu sinif hicbir mevcut mekanigi degistirmez/kopyalamaz - ya dogrudan
// mevcut sinifa delege eder (GetGameState -> LevelScanner.GetCurrentState,
// ResetLevel -> LevelManager.Instance.ResetLevel) ya da baska bir yerde private
// oldugu icin cagrilamayan, ama tamamen ayni ve side-effect'siz bir mantigi
// (GetAvailableActions, PlayerSimBrain.BuildCandidates ile birebir ayni) burada
// yeniden yazar. Hicbir yeni oyun kurali icat edilmedi.
//
// Bu turda:
//   - MultiLevelTestRunner.cs'e DOKUNULMADI ve bu adapter ona BAGLANMADI.
//   - Sahneye eklenmedi, hicbir yerden cagrilmiyor - sadece bagimsiz olarak
//     derlenebilir durumda.
//   - "levels" ve "scanner" alanlari, MultiLevelTestRunner'daki ayni isimli
//     alanlardan BAGIMSIZ, adapter'a ozel Inspector alanlaridir (kasitli
//     gecici duplikasyon - asagidaki rapor notuna bakin).
// ---------------------------------------------------------------------------

public class PuzzleGameAdapter : MonoBehaviour, IGameAdapter, IGameLevelProvider, IGameStateReader, IGameActionExecutor
{
    [Header("Mevcut MultiLevelTestRunner ile ayni sekilde Inspector'dan baglanir")]
    public LevelScanner scanner;
    public List<LevelTestConfig> levels = new List<LevelTestConfig>();

    // IGameAdapter: alt sozlesmeleri disari acar. Bu sinif ucunu de kendisi
    // implement ettigi icin sadece kendini dondurur.
    public IGameLevelProvider Levels => this;
    public IGameStateReader StateReader => this;
    public IGameActionExecutor ActionExecutor => this;

    // -----------------------------------------------------------------
    // IGameLevelProvider
    // MultiLevelTestRunner.SetActiveLevel() / ResetLevel(LevelTestConfig)
    // ile BIREBIR ayni mantik; sadece level kimligi artik string.
    // -----------------------------------------------------------------

    public IEnumerable<string> GetLevels()
    {
        List<string> names = new List<string>();
        foreach (LevelTestConfig config in levels)
        {
            names.Add(config.levelName);
        }
        return names;
    }

    public void LoadLevel(string levelId)
    {
        // Guvenlik notu: eskiden levelId hicbir config ile eslesmezse bu metot
        // sessizce hicbir sey yapmiyordu (foreach bos donuyordu, uyari yoktu).
        // ResetLevel() ile tutarli olmasi ve "sessizce yanlis davranis" riskini
        // azaltmak icin ayni acik uyari burada da eklendi. Eslesme varsa davranis
        // birebir eskisiyle aynidir.
        if (FindLevelConfig(levelId) == null)
        {
            Debug.LogWarning("PuzzleGameAdapter: '" + levelId + "' adinda bir level bulunamadi, LoadLevel hicbir sey yapmadi.");
        }

        foreach (LevelTestConfig config in levels)
        {
            bool shouldBeActive = config.levelName == levelId;
            config.levelRoot.SetActive(shouldBeActive);
        }
    }

    public void ResetLevel(string levelId)
    {
        LevelTestConfig target = FindLevelConfig(levelId);
        if (target == null)
        {
            Debug.LogWarning("PuzzleGameAdapter: '" + levelId + "' adinda bir level bulunamadi.");
            return;
        }

        LevelManager.Instance.ResetLevel();

        foreach (KeyInteraction key in target.keysToReset)
        {
            key.ResetState();
        }
    }

    private LevelTestConfig FindLevelConfig(string levelId)
    {
        foreach (LevelTestConfig config in levels)
        {
            if (config.levelName == levelId)
            {
                return config;
            }
        }

        return null;
    }

    // -----------------------------------------------------------------
    // IGameStateReader
    // LevelScanner.GetCurrentState()'e DOGRUDAN delege eder. Win/Lose dahil
    // (GameState.Status = LevelManager.Instance.CurrentStatus) hicbir sey
    // kaybolmaz/degismez.
    // -----------------------------------------------------------------

    public GameState GetGameState()
    {
        return scanner.GetCurrentState();
    }

    // -----------------------------------------------------------------
    // IGameActionExecutor
    // GetAvailableActions(): PlayerSimBrain.BuildCandidates() ile BIREBIR ayni
    // mantik (interactable + gameObject.name -> ActionCandidate). BuildCandidates
    // private oldugu ve PlayerSimBrain.cs bu turda degistirilemedigi icin
    // dogrudan cagrilamiyor; ayni, side-effect'siz mantik burada tekrar yazildi.
    // Execute(): mevcut BotBrain/PlayerSimBrain'in Executing state'inde yaptigi
    // ile ayni - dogrudan IInteractable.Interact() cagrisi.
    // -----------------------------------------------------------------

    public List<ActionCandidate> GetAvailableActions()
    {
        List<ActionCandidate> candidates = new List<ActionCandidate>();
        GameState state = GetGameState();

        foreach (IInteractable interactable in state.Interactables)
        {
            MonoBehaviour mb = interactable as MonoBehaviour;
            string id = mb != null ? mb.gameObject.name : null;
            candidates.Add(new ActionCandidate(interactable, id));
        }

        return candidates;
    }

    public void Execute(ActionCandidate action)
    {
        action.Interactable.Interact();
    }

    // -----------------------------------------------------------------
    // IGameAdapter - opsiyonel ek metrik noktasi
    // Bu oyun icin su an ek/ozel bir metrik yok; bos dictionary donuluyor.
    // Core rapor semasina (LevelTestReport, MultiProfileSummaryReport vb.)
    // hicbir etkisi yoktur.
    // -----------------------------------------------------------------

    public Dictionary<string, float> GetCustomMetrics()
    {
        return new Dictionary<string, float>();
    }

    // GENERIC FRAMEWORK - ADIM 6: bu oyun (Key/Door tarzi fact/precondition/effect
    // tabanli puzzle) ActionGenerator+SearchEngine BFS'i ile tam uyumlu - solver
    // gecisi HER ZAMAN oldugu gibi calismaya devam etmeli. Bu yuzden true.
    public bool SupportsSolver => true;
}
