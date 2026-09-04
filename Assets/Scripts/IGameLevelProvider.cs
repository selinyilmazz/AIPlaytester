using System.Collections.Generic;

// ---------------------------------------------------------------------------
// GENERIC FRAMEWORK - ADIM 1 (sadece tanim, henuz hicbir sinif implement etmiyor)
//
// Framework'un, oyundaki level'lari listeleyip yukleyebilmesi/resetleyebilmesi
// icin kullanacagi sozlesme. Mevcut oyunda bunun karsiligi MultiLevelTestRunner
// icindeki "levels" listesi ve SetActiveLevel()/ResetLevel() metotlaridir - ama
// bu interface o mantiga BAGLANMADI, sadece sozlesme tanimlandi.
//
// Tip secimi: yeni bir "LevelHandle" sinifi uretilmedi. Mevcut projede level'lar
// zaten baska hicbir ozel tipe ihtiyac duyulmadan, tutarli sekilde string ile
// kimlikleniyor (LevelTestConfig.levelName, TestReporter.SaveReport'un levelName
// parametresi, LevelProfileSummary.LevelName, LastSolverSummaries/LastPlayerSummaries
// dictionary anahtarlari - hepsi string). Bu yuzden level kimligi icin de ayni
// konvansiyona uyularak dogrudan string kullanildi.
// ---------------------------------------------------------------------------

public interface IGameLevelProvider
{
    /// <summary>Test edilebilir tum level'larin kimliklerini dondurur.</summary>
    IEnumerable<string> GetLevels();

    /// <summary>Belirtilen level'i aktif/yuklu hale getirir.</summary>
    void LoadLevel(string levelId);

    /// <summary>Belirtilen level'i baslangic durumuna resetler.</summary>
    void ResetLevel(string levelId);
}
