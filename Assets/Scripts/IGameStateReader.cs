// ---------------------------------------------------------------------------
// GENERIC FRAMEWORK - ADIM 1 (sadece tanim, henuz hicbir sinif implement etmiyor)
//
// Framework'un, uzerinde calistigi oyunun anlik durumunu okuyabilmesi icin
// kullanacagi sozlesme. Mevcut oyunda bunun karsiligi LevelScanner.GetCurrentState()
// metodudur - ama bu interface o metoda BAGLANMADI, sadece gelecekte baglanabilmesi
// icin sozlesme tanimlandi.
//
// Tip secimi: GameState icin yeni bir tip uretilmedi. Mevcut GameState sinifi
// (Status + Interactables) zaten bu ihtiyaci tam karsiliyor, bu yuzden dogrudan
// kullanildi.
// ---------------------------------------------------------------------------

public interface IGameStateReader
{
    /// <summary>
    /// Oyunun su anki durumunu dondurur (LevelStatus + o an sahnede bulunan
    /// interactable'lar). Mevcut projede LevelScanner.GetCurrentState() ile
    /// ayni sekli tasir.
    /// </summary>
    GameState GetGameState();
}
