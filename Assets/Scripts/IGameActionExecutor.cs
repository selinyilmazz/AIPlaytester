using System.Collections.Generic;

// ---------------------------------------------------------------------------
// GENERIC FRAMEWORK - ADIM 1 (sadece tanim, henuz hicbir sinif implement etmiyor)
//
// Framework'un, oyunda hangi aksiyonlarin su an uygulanabilir oldugunu kesfedip
// bunlari uygulayabilmesi icin kullanacagi sozlesme. Mevcut oyunda bunun karsiligi
// ActionGenerator (kesif) ile IInteractable.Interact() (uygulama) kombinasyonudur -
// ama bu interface o siniflara BAGLANMADI, sadece sozlesme tanimlandi.
//
// Tip secimi: yeni bir "ActionHandle" sinifi uretilmedi. Mevcut projede zaten var
// olan ActionCandidate tipi (IInteractable + string ActionId) - PlayerSimBrain
// tarafindan kullaniliyor ama kendisi oyuna/player'a ozel hicbir alan tasimiyor,
// tamamen notr bir veri tasiyicisi - bu ihtiyaci tam karsiladigi icin dogrudan
// kullanildi.
// ---------------------------------------------------------------------------

public interface IGameActionExecutor
{
    /// <summary>
    /// Su anki durumda uygulanabilir aksiyonlarin listesini dondurur. Mevcut
    /// projede bu, ActionGenerator'in sahneyi tarayip IPlannable/IInteractable
    /// nesnelerden urettigi listeye karsilik gelir.
    /// </summary>
    List<ActionCandidate> GetAvailableActions();

    /// <summary>
    /// Verilen aksiyonu oyunda uygular. Mevcut projede bu,
    /// action.Interactable.Interact() cagrisina karsilik gelir.
    /// </summary>
    void Execute(ActionCandidate action);
}
