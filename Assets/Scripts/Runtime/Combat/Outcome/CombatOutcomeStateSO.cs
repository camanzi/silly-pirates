using System;
using UnityEngine;

/// <summary>
/// Unico punto di verità sull'esito del combattimento corrente. Ricalcata sullo stesso schema di
/// <see cref="CombatIntroStateSO"/>: setter privati, metodi-verbo, e un solo metodo di reset privato
/// richiamato sia da OnEnable (caricamento dell'asset in memoria) sia da ResetForNewCombat
/// (fra uno scarico e l'altro della scena di combattimento in un'architettura multiscena additiva).
/// </summary>
[CreateAssetMenu(fileName = "CombatOutcomeState", menuName = "Combat/Outcome/Combat Outcome State")]
public class CombatOutcomeStateSO : ScriptableObject, ICombatSessionResettable
{
    public CombatOutcome Outcome { get; private set; }
    public bool IsCombatOver => Outcome != CombatOutcome.None;

    public event Action<CombatOutcome> OnCombatResolved;

    private void OnEnable() => ResetOutcome();

    // Simmetrico a CombatIntroStateSO.ResetForNewCombat: se non richiamato esplicitamente qui,
    // il secondo combattimento della sessione partirebbe già "risolto" perché OnEnable di uno
    // ScriptableObject scatta una sola volta per sessione di Play/build, non ad ogni scena.
    public void ResetForNewCombat() => ResetOutcome();

    private void ResetOutcome() => Outcome = CombatOutcome.None;

    /// <summary>
    /// Fissa l'esito e notifica i listener (es. il pannello di fine combattimento). No-op se il
    /// combattimento è già risolto (l'esito non si sovrascrive) o se viene passato None (non è un
    /// esito valido da risolvere, è lo stato di partenza).
    /// </summary>
    public void Resolve(CombatOutcome outcome)
    {
        if (IsCombatOver) return;
        if (outcome == CombatOutcome.None) return;

        Outcome = outcome;
        OnCombatResolved?.Invoke(outcome);
    }
}
