using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Elenco esplicito e ispezionabile degli ScriptableObject con stato runtime da azzerare tra un
/// combattimento e il successivo. Niente reflection, niente auto-discovery: quello che finisce
/// nella lista si vede dall'Inspector, e OnValidate segnala subito un asset che non implementa il
/// contratto invece di fallire in silenzio a runtime.
///
/// SceneFlowDirector (Fase 2) chiama ResetForNewCombat() fra lo scarico della scena di combattimento
/// vecchia e il caricamento di quella nuova: resettare DOPO il load cancellerebbe le registrazioni
/// già fatte dagli OnEnable della scena appena caricata.
/// </summary>
[CreateAssetMenu(fileName = "CombatSession", menuName = "Scene Flow/Combat Session")]
public class CombatSessionSO : ScriptableObject
{
    [Tooltip("L'ordine di reset segue l'ordine della lista, ma per come è oggi nessun elemento " +
             "dipende da un altro: ogni ResetForNewCombat tocca solo il proprio stato. Se un domani " +
             "servisse una dipendenza d'ordine, è il segnale che quel reset sta facendo troppo — " +
             "meglio renderlo indipendente che documentare un ordine da rispettare qui.")]
    [SerializeField] private List<ScriptableObject> _resettables;

    public void ResetForNewCombat()
    {
        if (_resettables == null) return;

        for (int i = 0; i < _resettables.Count; i++)
        {
            ScriptableObject entry = _resettables[i];
            if (entry == null) continue;

            if (entry is ICombatSessionResettable resettable)
            {
                resettable.ResetForNewCombat();
            }
            else
            {
                Debug.LogError(
                    $"[{nameof(CombatSessionSO)}] '{entry.name}' non implementa " +
                    $"{nameof(ICombatSessionResettable)}: rimuovilo dalla lista o correggi il tipo, " +
                    "altrimenti resta invisibilmente non resettato tra un combattimento e l'altro.", this);
            }
        }
    }

    // Segnala subito in Inspector un asset trascinato per sbaglio, invece di scoprirlo al secondo
    // combattimento della sessione.
    private void OnValidate()
    {
        if (_resettables == null) return;

        for (int i = 0; i < _resettables.Count; i++)
        {
            ScriptableObject entry = _resettables[i];
            if (entry == null) continue;

            if (entry is not ICombatSessionResettable)
            {
                Debug.LogError(
                    $"[{nameof(CombatSessionSO)}] elemento #{i} ('{entry.name}') non implementa " +
                    $"{nameof(ICombatSessionResettable)}: asset sbagliato trascinato nella lista?", this);
            }
        }
    }
}
