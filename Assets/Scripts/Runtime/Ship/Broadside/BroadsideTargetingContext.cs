using System.Collections.Generic;

/// <summary>
/// Everything a <see cref="BroadsideTargetSelectorSO"/> may look at to pick the targets of one equipment.
/// The broadside's counterpart of AIContext.
/// </summary>
public readonly struct BroadsideTargetingContext
{
    public readonly OffensiveEquipment Equipment;
    public readonly AbilityBase Ability;

    /// <summary>How many targets to pick: IMultiTargetAbility.MaxTargets, otherwise 1.</summary>
    public readonly int TargetCount;

    /// <summary>The legal targets, already filtered by <see cref="BroadsideCandidateQuery"/>.</summary>
    public readonly IReadOnlyList<ITargettable> Candidates;

    public readonly TurnOrderDataSO TurnOrder;
    public readonly GridStateDataSO GridState;

    public BroadsideTargetingContext(OffensiveEquipment equipment, AbilityBase ability, int targetCount,
                                     IReadOnlyList<ITargettable> candidates,
                                     TurnOrderDataSO turnOrder, GridStateDataSO gridState)
    {
        Equipment = equipment;
        Ability = ability;
        TargetCount = targetCount;
        Candidates = candidates;
        TurnOrder = turnOrder;
        GridState = gridState;
    }

    public static int ResolveTargetCount(AbilityBase ability)
        => ability is IMultiTargetAbility multi ? multi.MaxTargets : 1;
}
