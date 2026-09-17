using System;
using UnityEngine;

/// <summary>
/// The single source of truth for the current combat's outcome. Modelled on the same scheme as
/// <see cref="CombatIntroStateSO"/>: private setters, verb methods, and one private reset method called
/// both from OnEnable (when the asset is loaded into memory) and from ResetForNewCombat (between one
/// unload of the combat scene and the next, in an additive multi-scene architecture).
/// </summary>
[CreateAssetMenu(fileName = "CombatOutcomeState", menuName = "Combat/Outcome/Combat Outcome State")]
public class CombatOutcomeStateSO : ScriptableObject, ICombatSessionResettable
{
    public CombatOutcome Outcome { get; private set; }
    public bool IsCombatOver => Outcome != CombatOutcome.None;

    public event Action<CombatOutcome> OnCombatResolved;

    private void OnEnable() => ResetOutcome();

    // Symmetrical to CombatIntroStateSO.ResetForNewCombat: without being called explicitly here, the
    // session's second combat would start already "resolved", because a ScriptableObject's OnEnable
    // fires once per Play/build session and not once per scene.
    public void ResetForNewCombat() => ResetOutcome();

    private void ResetOutcome() => Outcome = CombatOutcome.None;

    /// <summary>
    /// Fixes the outcome and notifies the listeners (e.g. the end-of-combat panel). A no-op when the
    /// combat is already resolved (an outcome is never overwritten) or when None is passed in (that is
    /// not a valid outcome to resolve to, it is the starting state).
    /// </summary>
    public void Resolve(CombatOutcome outcome)
    {
        if (IsCombatOver) return;
        if (outcome == CombatOutcome.None) return;

        Outcome = outcome;
        OnCombatResolved?.Invoke(outcome);
    }
}
