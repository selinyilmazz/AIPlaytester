using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiLevelTestRunner : MonoBehaviour
{
    public BotBrain solverBrain;
    public PlayerSimBrain playerBrain;

    public int solverTestCount = 5;
    public int playerTestCount = 100;

    public List<LevelTestConfig> levels;

    // GENERIC FRAMEWORK - ADIM 3 (Level Provider entegrasyonu) + ADIM 8 (adapter tipi
    // genellestirme). 'adapter' Inspector alani artik somut PuzzleGameAdapter DEGIL,
    // genel MonoBehaviour tipinde - boylece hem PuzzleGameAdapter hem CounterGameAdapter
    // (veya ileride IGameAdapter implement eden baska bir adapter) buraya surukleyip
    // birakilabilir (Unity Inspector interface alanlarini dogrudan serialize edemedigi
    // icin bu, projede en az degisiklikle calisan yontemdir). Gercek IGameAdapter
    // referansi Start()'ta 'adapter' alanindan CAST edilerek ozel 'adapterInterface'
    // alanina yazilir; SetActiveLevel()/AdapterHasLevel()/ResolveLevelsToRun()/
    // ResetLevel()/RunAllLevels() ARTIK SADECE adapterInterface'i kullanir. Alan ADI
    // ('adapter') DEGISMEDI - mevcut sahnedeki (SampleScene.unity) serialized referans
    // (PuzzleGameAdapter zaten bir MonoBehaviour oldugu icin) otomatik korunur.
    public MonoBehaviour adapter;
    private IGameAdapter adapterInterface;

    public static bool AllTestsCompleted = false;
    public static List<string> CompletedLevelNames = new List<string>();
    public static List<DifficultyReport> CompletedReports = new List<DifficultyReport>();

    // Dashboard progress state (read-only bilgi amacli, test mantigini etkilemez)
    public static bool IsRunning = false;
    public static string CurrentLevelName = "";
    public static string CurrentProfileName = "";
    public static int CurrentRunIndex = 0;
    public static int TotalRunsInBatch = 0;
    public static int CurrentLevelIndex = 0;
    public static int TotalLevels = 0;
    public static float OverallProgress = 0f;
    public static string CurrentStatus = "Idle";

    // Progress/run sayaclari (UI'nin dogrudan okuyabilmesi icin static, hesap mantigi ayni)
    public static int TotalRunUnits = 0;
    public static int CompletedRunUnits = 0;

    // Dashboard analytics state: zaten hesaplanmis MetricsSummary/MultiProfileSummaryReport
    // referanslarini UI'nin okuyabilmesi icin tutar. JSON'a hic yazilmaz, sadece UI icin.
    public static Dictionary<string, MetricsSummary> LastSolverSummaries = new Dictionary<string, MetricsSummary>();
    public static Dictionary<string, MetricsSummary> LastPlayerSummaries = new Dictionary<string, MetricsSummary>();
    public static MultiProfileSummaryReport LastSummary;

    // Dashboard config gorunumu icin, zaten var olan Inspector degerlerinin salt-okunur kopyasi.
    // Test mantigina hicbir etkisi yok, sadece UI'nin okuyabilmesi icin.
    public static int LastPlayerTestCount = 0;
    public static float LastSimulationSpeed = 0f;
    public static float LastStepDelay = 0f;
    public static float LastTimeoutSeconds = 0f;

    void Start()
    {
        // GENERIC FRAMEWORK - ADIM 8: adapterInterface iki yoldan biriyle doldurulur:
        // (1) Inspector'da 'adapter' elle atanmissa (herhangi bir MonoBehaviour,
        //     PuzzleGameAdapter da CounterGameAdapter da olabilir) buradan IGameAdapter'a
        //     CAST edilir. Atanan nesne IGameAdapter implement ETMIYORSA (yanlislikla
        //     baska bir script suruklenmisse) acik bir uyari basilir ve adapterInterface
        //     null kalir - sessiz/yanlis davranis yerine PlayerSimBrain.
        //     ResolveAdapterIfNeeded() ile AYNI temkinli yaklasim.
        // (2) 'adapter' bos birakilmissa, PlayerSimBrain.ResolveAdapterIfNeeded() ile
        //     BIREBIR AYNI teknik uygulanir: sahnedeki tum MonoBehaviour'lar taranir,
        //     IGameAdapter implement eden ilk nesne kullanilir. Artik somut
        //     PuzzleGameAdapter tipine BAGLI DEGIL - CounterGameAdapter (veya ileride
        //     baska bir adapter) da bu sekilde otomatik bulunur.
        // Sahnede uygun bir adapter yoksa (bugunku gibi) adapterInterface null kalir ve
        // asagidaki tum cagri noktalari eski "adapter == null" fallback mantigina
        // AYNEN duser - PuzzleGame davranisi sifir degisir.
        if (adapter != null)
        {
            adapterInterface = adapter as IGameAdapter;
            if (adapterInterface == null)
            {
                Debug.LogWarning("MultiLevelTestRunner: Inspector'da 'adapter' alanina atanan '" + adapter.name + "' IGameAdapter implement etmiyor - adapter yoksayilip fallback mantigi kullanilacak.");
            }
        }
        else
        {
            MonoBehaviour[] allObjects = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (MonoBehaviour obj in allObjects)
            {
                IGameAdapter candidate = obj as IGameAdapter;
                if (candidate != null)
                {
                    adapterInterface = candidate;
                    adapter = obj;
                    Debug.Log("MultiLevelTestRunner: 'adapter' Inspector'da atanmamisti, sahnede otomatik bulundu -> " + obj.GetType().Name);
                    break;
                }
            }
        }

        StartCoroutine(RunAllLevels());
    }

    IEnumerator RunAllLevels()
    {
        AllTestsCompleted = false;
        CompletedLevelNames.Clear();
        CompletedReports.Clear();

        IsRunning = true;
        CurrentStatus = "Starting...";
        CurrentLevelName = "";
        CurrentProfileName = "";
        CurrentRunIndex = 0;
        TotalRunsInBatch = 0;
        CurrentLevelIndex = 0;

        // GENERIC FRAMEWORK - ADIM 7 (level enumeration): dis dongunun kaynagi artik
        // ResolveLevelsToRun() uzerinden belirleniyor - adapter varsa VE adapter.Levels.
        // GetLevels() doluysa bu isim listesi ONCELIKLI kaynak olur; degilse (adapter
        // yok, GetLevels() bos, ya da hicbir isim eslesmiyorsa) AYNEN eski "levels"
        // listesine dusulur. TotalLevels/TotalRunUnits hesaplari asagida bu YENI
        // listenin gercek sayisina gore yapilir - boylece ilerleme (%) her zaman
        // gercekte calisacak run sayisiyla tutarli kalir.
        List<LevelTestConfig> levelsToRun = ResolveLevelsToRun();
        TotalLevels = levelsToRun.Count;
        OverallProgress = 0f;

        LastSolverSummaries.Clear();
        LastPlayerSummaries.Clear();
        LastSummary = null;

        LastPlayerTestCount = playerTestCount;
        if (playerBrain != null)
        {
            LastSimulationSpeed = playerBrain.simulationSpeed;
            LastStepDelay = playerBrain.stepDelay;
            LastTimeoutSeconds = playerBrain.timeoutSeconds;
        }

        int profileCount = System.Enum.GetValues(typeof(PlayerProfileType)).Length;

        // GENERIC FRAMEWORK - ADIM 6: solverSupported burada BIR KEZ hesaplanir ve
        // hem TotalRunUnits hesabina hem RunSingleLevel()'a parametre olarak
        // gecirilir - boylece "kac run yapilacagi" beklentisi ile "gercekte kac run
        // yapilacagi" HER ZAMAN tutarli olur. adapter == null durumunda (bugunku
        // PuzzleGame sahnesi dahil, adapter atanmamissa) eski davranis (solver hep
        // calisir) AYNEN korunur.
        bool solverSupported = adapterInterface == null || adapterInterface.SupportsSolver;
        int solverRunsPerLevel = solverSupported ? solverTestCount : 0;

        TotalRunUnits = TotalLevels * (solverRunsPerLevel + (profileCount * playerTestCount));
        if (TotalRunUnits <= 0)
        {
            TotalRunUnits = 1;
        }
        CompletedRunUnits = 0;

        MultiProfileSummaryReport multiProfileSummary = new MultiProfileSummaryReport();
        multiProfileSummary.Timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        LastSummary = multiProfileSummary;

        try
        {
            int levelIndex = 0;
            foreach (LevelTestConfig levelConfig in levelsToRun)
            {
                Debug.Log("###################################");
                Debug.Log("LEVEL TEST BASLIYOR: " + levelConfig.levelName);
                Debug.Log("###################################");

                CurrentLevelIndex = levelIndex;
                CurrentLevelName = levelConfig.levelName;
                CurrentStatus = "Testing " + levelConfig.levelName;

                SetActiveLevel(levelConfig);

                yield return StartCoroutine(RunSingleLevel(levelConfig, multiProfileSummary, solverSupported));

                levelIndex++;
            }

            TestReporter summaryReporter = new TestReporter();
            summaryReporter.SaveMultiProfileSummary(multiProfileSummary);

            AllTestsCompleted = true;
            CurrentStatus = "Completed";
            OverallProgress = 1f;
            Debug.Log("===== TUM LEVELLER TEST EDILDI =====");
        }
        finally
        {
            IsRunning = false;
        }
    }

    private void SetActiveLevel(LevelTestConfig activeLevel)
    {
        // GENERIC FRAMEWORK - ADIM 3/4: adapter atanmissa VE adapter'in kendi levels
        // listesi bu level'i gercekten biliyorsa, yukleme IGameAdapter.Levels uzerinden
        // yapilir. Adapter atanmamissa YA DA adapter.levels bu level'i icermiyorsa
        // (MultiLevelTestRunner.levels ile PuzzleGameAdapter.levels farkli/duplicate
        // yapilandirilmissa - bkz. class-level not), sessizce yanlis davranmak yerine
        // acikca uyarip ESKI, kanitlanmis mantiga geri donulur.
        if (adapterInterface != null)
        {
            if (AdapterHasLevel(activeLevel.levelName))
            {
                adapterInterface.Levels.LoadLevel(activeLevel.levelName);
                return;
            }

            Debug.LogWarning("MultiLevelTestRunner: adapter atanmis ama '" + activeLevel.levelName + "' adapter.levels icinde bulunamadi - guvenlik icin eski (fallback) SetActiveLevel mantigi kullanildi. PuzzleGameAdapter.levels listesini runner.levels ile ayni sekilde doldurun.");
        }

        foreach (LevelTestConfig levelConfig in levels)
        {
            bool shouldBeActive = levelConfig == activeLevel;
            levelConfig.levelRoot.SetActive(shouldBeActive);
        }
    }

    // GENERIC FRAMEWORK - ADIM 4: MultiLevelTestRunner.levels ile PuzzleGameAdapter.levels
    // su an iki ayri, senkronize olmayan yapilandirma noktasi (bkz. rapor). Bu metot,
    // adapter'a gecmeden once o levelin adapter tarafinda gercekten taniniyor olup
    // olmadigini kontrol eder - boylece yapilandirma eksik/farkliysa sessiz hata yerine
    // acik bir fallback + uyari olusur.
    private bool AdapterHasLevel(string levelId)
    {
        if (adapterInterface == null)
        {
            return false;
        }

        foreach (string id in adapterInterface.Levels.GetLevels())
        {
            if (id == levelId)
            {
                return true;
            }
        }

        return false;
    }

    // GENERIC FRAMEWORK - ADIM 7: MultiLevelTestRunner
    //     -> IGameAdapter
    //         -> IGameLevelProvider.GetLevels()
    //             -> Game Levels
    // hedefine ulasmak icin dis dongunun ENUMERATION KAYNAGINI belirler. Guvenlik
    // kurali AdapterHasLevel() ile AYNI felsefeyi tasir: adapter'a GUVEN, ama
    // DOGRULAMADAN degil. GetLevels()'in dondurdugu her isim icin gercek
    // LevelTestConfig (levelRoot/keysToReset icin gerekli) hala runner.levels
    // icinden aranir - IGameLevelProvider bu veriyi tasimadigi icin bu arama
    // ZORUNLU. Hicbir eslesme bulunamazsa (adapter.levels ile runner.levels tam
    // senkronsuzsa) sessizce bos bir liste donmek yerine ESKI, kanitlanmis
    // "levels" listesine acikca geri donulur.
    private List<LevelTestConfig> ResolveLevelsToRun()
    {
        if (adapterInterface == null)
        {
            return levels;
        }

        List<string> adapterLevelNames = new List<string>(adapterInterface.Levels.GetLevels());
        if (adapterLevelNames.Count == 0)
        {
            return levels;
        }

        List<LevelTestConfig> resolved = new List<LevelTestConfig>();
        foreach (string levelName in adapterLevelNames)
        {
            LevelTestConfig match = FindLevelConfigByName(levelName);
            if (match != null)
            {
                resolved.Add(match);
            }
            else
            {
                Debug.LogWarning("MultiLevelTestRunner: adapter.Levels.GetLevels() icinde '" + levelName + "' var ama bu isimde bir LevelTestConfig runner.levels icinde bulunamadi - bu level ATLANIYOR. PuzzleGameAdapter.levels listesini runner.levels ile ayni sekilde doldurun.");
            }
        }

        if (resolved.Count == 0)
        {
            Debug.LogWarning("MultiLevelTestRunner: adapter.Levels.GetLevels() icindeki hicbir level runner.levels ile eslesmedi - guvenlik icin eski (fallback) level listesi kullanildi.");
            return levels;
        }

        return resolved;
    }

    private LevelTestConfig FindLevelConfigByName(string levelName)
    {
        foreach (LevelTestConfig levelConfig in levels)
        {
            if (levelConfig.levelName == levelName)
            {
                return levelConfig;
            }
        }

        return null;
    }

    private IEnumerator RunSingleLevel(LevelTestConfig levelConfig, MultiProfileSummaryReport multiProfileSummary, bool solverSupported)
    {
        List<TestResult> solverResults = new List<TestResult>();

        // GENERIC FRAMEWORK - ADIM 6: solverSupported true iken (bugunku PuzzleGame
        // dahil, adapter atanmamissa da true) bu blok ONCEKI KODLA BIREBIR AYNI
        // calisir - tek fark, artik bir if bloguna sarilmis olmasi. false iken
        // ActionGenerator/SearchEngine hic cagrilmaz, solver dongu atlanir,
        // solverResults bos kalir.
        if (solverSupported)
        {
            ActionGenerator actionGenerator = new ActionGenerator();
            List<ActionDefinition> actions = actionGenerator.GenerateActions();

            SearchEngine engine = new SearchEngine();
            List<string> solution = engine.FindSolutionBFS(actions, solverBrain.maxSteps);

            if (solution == null)
            {
                Debug.Log("SearchEngine: " + levelConfig.levelName + " icin cozum bulunamadi.");
            }
            else
            {
                Debug.Log("SearchEngine: " + levelConfig.levelName + " cozumu -> " + string.Join(" -> ", solution));
            }

            CurrentProfileName = "Solver";
            CurrentRunIndex = 0;
            TotalRunsInBatch = solverTestCount;

            for (int i = 0; i < solverTestCount; i++)
            {
                CurrentRunIndex = i;
                CurrentStatus = "Testing " + levelConfig.levelName + " - Solver";

                ResetLevel(levelConfig);

                TestResult result = null;
                yield return StartCoroutine(solverBrain.RunTest(r => result = r, solution));
                solverResults.Add(result);

                CompletedRunUnits++;
                UpdateOverallProgress();
            }
        }
        else
        {
            Debug.Log("MultiLevelTestRunner: '" + levelConfig.levelName + "' icin adapter solver'i desteklemiyor (SupportsSolver=false) - solver gecisi atlanip sadece player-profile testleri calistirilacak.");
            TotalRunsInBatch = 0;
        }

        // MetricsCollector'a HICBIR DEGISIKLIK YAPILMADI: solverResults bos bir liste
        // olarak gelse bile Summarize() bunu guvenli sekilde (TotalTests=0, tum
        // oranlar=0) isliyor - bu, "solver verisi yok" durumunun DifficultyAnalyzer'a
        // dogru/durust bir sekilde yansimasini saglar.
        MetricsCollector collector = new MetricsCollector();
        MetricsSummary solverSummary = collector.Summarize(solverResults);
        LastSolverSummaries[levelConfig.levelName] = solverSummary;

        TestReporter reporter = new TestReporter();

        LevelProfileSummary levelProfileSummary = new LevelProfileSummary();
        levelProfileSummary.LevelName = levelConfig.levelName;

        foreach (PlayerProfileType profile in System.Enum.GetValues(typeof(PlayerProfileType)))
        {
            playerBrain.profileType = profile;
            playerBrain.ResetMemory();

            CurrentProfileName = profile.ToString();
            CurrentRunIndex = 0;
            TotalRunsInBatch = playerTestCount;

            List<TestResult> playerResults = new List<TestResult>();
            for (int i = 0; i < playerTestCount; i++)
            {
                CurrentRunIndex = i;
                CurrentStatus = "Testing " + levelConfig.levelName + " - " + profile;

                ResetLevel(levelConfig);

                TestResult result = null;
                yield return StartCoroutine(playerBrain.RunTest(r => result = r));
                playerResults.Add(result);

                CompletedRunUnits++;
                UpdateOverallProgress();
            }

            MetricsSummary playerSummary = collector.Summarize(playerResults);
            LastPlayerSummaries[levelConfig.levelName + " - " + profile] = playerSummary;

            DifficultyAnalyzer analyzer = new DifficultyAnalyzer();
            DifficultyReport report = analyzer.Analyze(solverSummary, playerSummary);

            Debug.Log(levelConfig.levelName + " [" + profile + "] SONUC -> Solver: " + solverSummary.SuccessRate.ToString("F1") + "%, Player: " + playerSummary.SuccessRate.ToString("F1") + "%, Difficulty: " + report.DifficultyScore.ToString("F1") + " (" + report.Classification + ")");

            reporter.SaveReport(levelConfig.levelName, solverSummary, playerSummary, report, solverResults, playerResults, profile.ToString());

            CompletedLevelNames.Add(levelConfig.levelName + " - " + profile);
            CompletedReports.Add(report);

            ProfileSummaryEntry entry = new ProfileSummaryEntry();
            entry.Profile = profile.ToString();
            entry.SuccessRate = playerSummary.SuccessRate;
            entry.AverageSteps = playerSummary.AverageSteps;
            entry.AverageWrongMoves = playerSummary.AverageWrongMoves;
            entry.DifficultyScore = report.DifficultyScore;
            entry.Classification = report.Classification;
            levelProfileSummary.Profiles.Add(entry);
        }

        multiProfileSummary.Levels.Add(levelProfileSummary);
    }

    private void UpdateOverallProgress()
    {
        OverallProgress = TotalRunUnits > 0 ? (float)CompletedRunUnits / TotalRunUnits : 1f;
    }

    private void ResetLevel(LevelTestConfig levelConfig)
    {
        // GENERIC FRAMEWORK - ADIM 3/4: SetActiveLevel() ile ayni guvenlik kurali -
        // adapter atanmis VE bu level adapter.levels icinde biliniyorsa adapter
        // uzerinden calisir; degilse acik uyariyla ESKI mantiga geri doner.
        if (adapterInterface != null)
        {
            if (AdapterHasLevel(levelConfig.levelName))
            {
                adapterInterface.Levels.ResetLevel(levelConfig.levelName);
                return;
            }

            Debug.LogWarning("MultiLevelTestRunner: adapter atanmis ama '" + levelConfig.levelName + "' adapter.levels icinde bulunamadi - guvenlik icin eski (fallback) ResetLevel mantigi kullanildi.");
        }

        LevelManager.Instance.ResetLevel();

        foreach (KeyInteraction key in levelConfig.keysToReset)
        {
            key.ResetState();
        }
    }
}
