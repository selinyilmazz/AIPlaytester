using System.Collections.Generic;

// ---------------------------------------------------------------------------
// GENERIC FRAMEWORK - ADIM 1 (sadece tanim, henuz hicbir sinif implement etmiyor)
//
// Framework'un tek bir oyuna baglandigi ust seviye sozlesme. IGameLevelProvider,
// IGameStateReader ve IGameActionExecutor'i bir araya getirir. Ileride
// (bu turda DEGIL) MultiLevelTestRunner'in somut oyun siniflarina (LevelScanner,
// ActionGenerator, sahne referanslari) dogrudan bagli olmak yerine sadece bu
// arayuze baglanmasi hedefleniyor.
//
// Bu turda hicbir sinif bu arayuzu implement etmiyor; MultiLevelTestRunner.cs
// dahil hicbir mevcut dosyaya dokunulmadi ve hicbir yerde kullanilmiyor.
//
// ISolverStrategy bu ADIM'a KASITLI OLARAK dahil edilmedi - bir sonraki asamada
// ele alinacak.
// ---------------------------------------------------------------------------

public interface IGameAdapter
{
    IGameLevelProvider Levels { get; }
    IGameStateReader StateReader { get; }
    IGameActionExecutor ActionExecutor { get; }

    /// <summary>
    /// Oyuna ozel, core MetricsSummary/DifficultyReport semasinin disinda kalan
    /// opsiyonel ek metrikler icin genisletme noktasi. Ozel metrigi olmayan
    /// adapter'lar bos bir dictionary donebilir. Core rapor semasina
    /// (LevelTestReport, MultiProfileSummaryReport vb.) hicbir etkisi yoktur -
    /// bu veriler JsonUtility ile serialize edilen mevcut siniflara eklenmez.
    /// </summary>
    Dictionary<string, float> GetCustomMetrics();

    /// <summary>
    /// GENERIC FRAMEWORK - ADIM 6: bu oyunun, MultiLevelTestRunner'in "mukemmel
    /// cozum" arayan solver gecisini (ActionGenerator + SearchEngine BFS) anlamli
    /// sekilde destekleyip desteklemedigini bildirir. true donen bir adapter icin
    /// solver gecisi HICBIR SEKILDE degismez (PuzzleGameAdapter -> true). false
    /// donen bir adapter icin MultiLevelTestRunner solver adimini atlar ve sadece
    /// player-profile testlerine devam eder. Bu, fact/precondition/effect tabanli
    /// BFS'in dogal olarak uymadigi oyun turleri (orn. surekli fizik tabanli
    /// platformer) icin gerekli bir "opt-out" noktasidir.
    /// </summary>
    bool SupportsSolver { get; }
}
