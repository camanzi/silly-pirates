using System.Collections.Generic;

/// <summary>
/// Builds the list of legal broadside targets: living enemies in the turn queue that the ability accepts
/// through <see cref="AbilityBase.IsValidTarget"/>. Keeping that last check means the per-ability rules
/// already written (the net only catches turn agents) apply to the broadside too, with no duplication.
/// </summary>
public static class BroadsideCandidateQuery
{
    public static void Collect(TurnOrderDataSO turnOrder, IInteractableElement caster, AbilityBase ability,
                               List<ITargettable> results)
    {
        results.Clear();
        if (turnOrder == null || ability == null) return;

        foreach (EntityTurnState state in turnOrder.TurnQueue)
        {
            if (state.Agent is not HostileCharacter) continue;
            if (state.Agent is not ITargettable target) continue;
            if (!IsLegalTarget(caster, ability, target)) continue;
            if (!results.Contains(target)) results.Add(target);
        }
    }

    /// <summary>
    /// Whether a target (picked earlier, e.g. at awakening) can still be shot now: it still exists, is
    /// alive when it has health at all, and the ability still accepts it.
    /// </summary>
    public static bool IsLegalTarget(IInteractableElement caster, AbilityBase ability, ITargettable target)
    {
        if (!IsAlive(target)) return false;
        if (ability == null) return true;

        object cache = null;
        var td = new TargetingData { selectedTarget = target };
        return ability.IsValidTarget(caster, td, ref cache);
    }

    /// <summary>Destroyed Unity objects compare equal to null only through UnityEngine.Object.</summary>
    internal static bool IsAlive(ITargettable target)
    {
        if (target == null) return false;
        if (target is UnityEngine.Object unityObject && unityObject == null) return false;
        if (target is IHealthOwner ho && (ho.Health == null || !ho.Health.IsAlive)) return false;
        return true;
    }
}
