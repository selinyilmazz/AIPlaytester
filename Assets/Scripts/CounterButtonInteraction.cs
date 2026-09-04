using UnityEngine;

// ---------------------------------------------------------------------------
// SECOND GAME PROOF - CounterButtonInteraction
//
// CounterGame'in tek etkilesim birimi: tiklandiginda sabit bir delta degerini
// CounterGameAdapter'daki sayac state'ine uygular.
//
// BILINCLI OLARAK sadece IInteractable implement eder:
//   - IPlannable EKLENMEDI: bu level solver'i desteklemiyor (CounterGameAdapter.
//     SupportsSolver = false), ActionGenerator bu levelde hic cagrilmiyor,
//     dolayisiyla fact/precondition/effect tanimina ihtiyac yok.
//   - IResettable EKLENMEDI: sayac state'i CounterGameAdapter'da tutuluyor,
//     butonun kendisi hicbir state tasimiyor - reset CounterGameAdapter.
//     ResetLevel() icinde (currentValue = 0) yapiliyor, buton bazinda degil.
// ---------------------------------------------------------------------------

public class CounterButtonInteraction : MonoBehaviour, IInteractable
{
    [Tooltip("Bu butona tiklandiginda/Execute edildiginde sayaca eklenecek deger (orn. +1, +5, -1)")]
    public int delta;

    [Tooltip("Bu butonun bagli oldugu CounterGameAdapter - sayac state'i buraya yazilir")]
    public CounterGameAdapter adapter;

    void OnMouseDown()
    {
        Interact();
    }

    public void Interact()
    {
        if (adapter == null)
        {
            Debug.LogWarning("CounterButtonInteraction: '" + gameObject.name + "' icin adapter atanmamis, Interact() hicbir sey yapmadi.");
            return;
        }

        Debug.Log("CounterButtonInteraction: '" + gameObject.name + "' tiklandi, delta=" + delta);
        adapter.ApplyDelta(delta);
    }
}
