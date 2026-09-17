using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Cell Cost Registry", menuName = "Grid/Cell Cost Registry")]
public class CellCostRegistrySO : ScriptableObject
{
    private readonly List<ICellCostModifier> _modifiers = new();

    // An anti-duplicate guard: without it, a repeated Register on the same modifier (a script
    // recompilation in Play Mode, or a defensive re-registration after ResetForNewCombat) makes
    // GetMovementCost add the same contribution several times over.
    public void Register(ICellCostModifier modifier)
    {
        if (modifier == null || _modifiers.Contains(modifier)) return;
        _modifiers.Add(modifier);
    }

    public void Unregister(ICellCostModifier modifier) => _modifiers.Remove(modifier);

    public int GetMovementCost(Vector3Int cell)
    {
        int cost = 1;
        for (int i = 0; i < _modifiers.Count; i++)
            cost += _modifiers[i].GetAdditionalCost(cell);
        return Mathf.Max(0, cost);
    }

    // This DELIBERATELY does not implement ICombatSessionResettable, and must not be put in
    // CombatSessionSO's _resettables list. Today's registrants (PathOfStarDataSO, SlimyCellDataSO) are SO
    // assets: they live as long as the application and never become dead references, so clearing the list
    // between one combat and the next repairs nothing and would in fact wipe still-valid registrations —
    // their OnEnable does not fire again (the asset is already in memory) and their contribution to the
    // cost would vanish for the rest of the session. Correctness across repeated loads comes from the
    // symmetrical Register/Unregister protocol, as it does for RuntimeAnchorSO.
    // If a modifier is ever a scene object, it has to unregister in its own OnDisable: that is the
    // contract, not a centralized reset.
}
