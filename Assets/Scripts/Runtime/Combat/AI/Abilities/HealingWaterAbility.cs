using UnityEngine;

[CreateAssetMenu(fileName = "HealingWater Ability", menuName = "Abilities/Enemy/Healing Water Ability")]
public class HealingWaterAbility : EnemyAbilityBase, IDefensiveAbility
{
    [Header("Healing Water configs")]
    [SerializeField] private float _fixedHeal = 20f;
    [SerializeField] private float _percentHeal = 0.10f;
    [SerializeField] private float _healThreshold = 0.75f;
    [Tooltip("Full turns with different abilities needed before this ability can be used again.")]
    [SerializeField] private int _requiredAlternateTurns = 2;
    [SerializeField] private GameObject _projectile;

    protected override bool MeetsPreconditions(AIContext context)
    {
        if (!base.MeetsPreconditions(context)) return false;
        return !context.Caster.AbilityCooldowns.TryGetValue(this, out int cd) || cd == 0;
    }

    protected override float ComputeScore(AIContext context, out TargetingData targeting)
    {
        ITargettable bestTarget = null;
        float lowestRatio = float.MaxValue;

        foreach (var state in context.TurnOrder.TurnQueue)
        {
            if (state.Agent is not HostileCharacter hostile) continue;
            if (!hostile.Health.IsAlive) continue;

            float ratio = hostile.Health.CurrentHp / hostile.Health.MaxHp;
            if (ratio < _healThreshold && ratio < lowestRatio)
            {
                lowestRatio = ratio;
                bestTarget = hostile;
            }
        }

        if (bestTarget == null)
        {
            targeting = TargetingData.Empty;
            return float.NegativeInfinity;
        }

        targeting = new TargetingData(
            bestTarget.Transform.position,
            default,
            true,
            bestTarget
        );
        return 1f;
    }

    public override bool CanExecute(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        return targetingData.HasValue &&
               targetingData.Value.selectedTarget is IHealthOwner ho &&
               ho.Health.IsAlive;
    }

    public override ICommand CreateCommand(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        var hostile = (HostileCharacter)caster;
        hostile.AbilityCooldowns[this] = _requiredAlternateTurns + 1;

        var target = (ITargettable)targetingData.Value.selectedTarget;
        float healAmount = _fixedHeal;
        if (target is IHealthOwner hw)
            healAmount += hw.Health.MaxHp * _percentHeal;

        return new HealingWaterCommand(hostile, target, healAmount, _projectile, trajectoryConfigData);
    }
}
