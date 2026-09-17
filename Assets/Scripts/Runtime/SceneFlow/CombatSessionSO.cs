using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An explicit, inspectable list of the ScriptableObjects carrying runtime state that has to be cleared
/// between one combat and the next. No reflection, no auto-discovery: whatever ends up in the list is
/// visible in the Inspector, and OnValidate flags an asset that does not implement the contract straight
/// away instead of failing silently at runtime.
///
/// SceneFlowDirector (Phase 2) calls ResetForNewCombat() between unloading the old combat scene and
/// loading the new one: resetting AFTER the load would wipe the registrations the incoming scene's
/// OnEnable calls have just made.
/// </summary>
[CreateAssetMenu(fileName = "CombatSession", menuName = "Scene Flow/Combat Session")]
public class CombatSessionSO : ScriptableObject
{
    [Tooltip("The reset order follows the list's order, but as things stand no element depends on " +
             "another: each ResetForNewCombat touches only its own state. If an order dependency were " +
             "ever needed, that is the signal that the reset in question is doing too much — better to " +
             "make it independent than to document an order to respect here.")]
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
                    $"[{nameof(CombatSessionSO)}] '{entry.name}' does not implement " +
                    $"{nameof(ICombatSessionResettable)}: remove it from the list or fix the type, " +
                    "otherwise it stays invisibly un-reset between one combat and the next.", this);
            }
        }
    }

    // Flags an asset dragged in by mistake right there in the Inspector, instead of discovering it in
    // the session's second combat.
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
                    $"[{nameof(CombatSessionSO)}] item #{i} ('{entry.name}') does not implement " +
                    $"{nameof(ICombatSessionResettable)}: wrong asset dragged into the list?", this);
            }
        }
    }
}
