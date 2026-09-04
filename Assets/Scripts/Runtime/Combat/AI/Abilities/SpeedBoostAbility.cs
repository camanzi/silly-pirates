using UnityEngine;

[CreateAssetMenu(fileName = "SpeedBoost Ability", menuName = "Abilities/Enemy/Speed Boost Ability")]
public class SpeedBoostAbility : EnemyAbilityBase, IDefensiveAbility
{
    [Header("SpeedBoost Ability configs")]
    [Tooltip("Full turns with different abilities needed before this ability can be used again.")]
    [SerializeField] private int _requiredAlternateTurns = 4;
    [SerializeField] private SpeedBoostPassiveSO _passiveSO;

    protected override bool MeetsPreconditions(AIContext context)
    {
        if (!base.MeetsPreconditions(context)) return false;
        return !context.Caster.AbilityCooldowns.TryGetValue(this, out int cd) || cd == 0;
    }

    protected override float ComputeScore(AIContext context, out TargetingData targeting)
    {
        HostileCharacter bossTarget = null;
        HostileCharacter healerTarget = null;
        HostileCharacter partyMemberTarget = null;

        foreach (var state in context.TurnOrder.TurnQueue)
        {
            if (state.Agent is not HostileCharacter hostile) continue;
            if (!hostile.Health.IsAlive) continue;
            if (SpeedBoostPassiveSO.IsActiveOn(hostile)) continue;

            switch (hostile.Role)
            {
                case EnemyRole.Boss:
                    bossTarget = hostile;
                    break;
                case EnemyRole.Healer:
                    healerTarget ??= hostile;
                    break;
                case EnemyRole.PartyMember:
                    partyMemberTarget ??= hostile;
                    break;
            }
        }

        HostileCharacter chosen = bossTarget ?? healerTarget ?? partyMemberTarget;

        if (chosen == null)
        {
            targeting = TargetingData.Empty;
            return float.NegativeInfinity;
        }

        targeting = new TargetingData(chosen.Transform.position, default, true, chosen);
        return 1f;
    }

    public override bool CanExecute(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        return targetingData.HasValue &&
               targetingData.Value.selectedTarget is HostileCharacter t &&
               t.Health.IsAlive &&
               t.PassiveAbilityController != null &&
               !t.PassiveAbilityController.HasPassive<SpeedBoostPassiveSO>();
    }

    public override ICommand CreateCommand(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        var hostile = (HostileCharacter)caster;
        hostile.AbilityCooldowns[this] = _requiredAlternateTurns + 1;

        var target = (HostileCharacter)targetingData.Value.selectedTarget;
        return new SpeedBoostCommand(hostile, target, _passiveSO);
    }
}
