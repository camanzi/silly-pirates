using System.Collections.Generic;

/// <summary>
/// Announces that an ability is about to be executed against some targets, and that the execution is
/// over. Raised at the two (and only two) places where a command is queued and executed: ExecutionStateSO
/// for the player, EnemyTurnDriver for the enemies.
///
/// It is what lets the targets react BEFORE the impact — the damage lands late inside the command, after
/// the projectile's flight — something a HealthBehaviorSO, which only sees the hit once it has arrived,
/// cannot do. The twin of AbilityExecutionCue, which carries the same data to the camera direction: kept
/// separate because that one has no closing event, and binding to the camera director would be the wrong
/// coupling.
/// </summary>
public struct AbilityThreatCue
{
    /// <summary>false = no threat in flight: every reactor goes back to rest.</summary>
    public bool Active;

    public AbilityBase Ability;
    public IInteractableElement Caster;
    public IReadOnlyList<ITargettable> Targets;
    public AbilityIntent Intent;

    /// <summary><see cref="DamageType.None"/> when the ability is not offensive or cannot name its own element.</summary>
    public DamageType Element;

    public static AbilityThreatCue Begin(AbilityBase ability, IInteractableElement caster,
                                         IReadOnlyList<ITargettable> targets)
    {
        return new AbilityThreatCue
        {
            Active   = true,
            Ability  = ability,
            Caster   = caster,
            Targets  = targets,
            Intent   = ability.GetIntent(),
            Element  = ability is IOffensiveAbility offensive
                           ? offensive.ResolveDamageElement(caster)
                           : DamageType.None
        };
    }

    /// <summary>
    /// A global close, not a per-target one: it holds because the turn loop is sequential and Execution is
    /// a single state, so there are never two overlapping executions to tell apart.
    /// </summary>
    public static AbilityThreatCue End => default;
}
