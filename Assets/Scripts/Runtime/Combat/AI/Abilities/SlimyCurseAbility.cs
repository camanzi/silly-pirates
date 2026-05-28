using UnityEngine;

[CreateAssetMenu(fileName = "SlimyCurse Ability", menuName = "Abilities/Enemy/Slimy Curse Ability")]
public class SlimyCurseAbility : EnemyAbilityBase
{
    [Header("SlimyCurse ability configs")]
    [SerializeField] private SlimyCursePassiveSO _cursePassive;

    protected override float ComputeScore(AIContext context, out TargetingData targeting)
    {
        GridCharacter preferredTarget = null;
        GridCharacter fallbackTarget = null;
        float preferredDist = float.MaxValue;
        float fallbackDist = float.MaxValue;

        foreach (var state in context.TurnOrder.TurnQueue)
        {
            if (state.Agent is HostileCharacter) continue;
            if (state.Agent is not IHealthOwner ho || !ho.Health.IsAlive) continue;
            if (state.Agent is not GridCharacter character) continue;

            float dist = Vector3.Distance(context.Caster.Transform.position, character.Transform.position);

            if (!SlimyCursePassiveSO.IsActiveOn(character))
            {
                if (dist < preferredDist) { preferredTarget = character; preferredDist = dist; }
            }
            else
            {
                if (dist < fallbackDist) { fallbackTarget = character; fallbackDist = dist; }
            }
        }

        GridCharacter chosen = preferredTarget ?? fallbackTarget;
        if (chosen == null)
        {
            targeting = TargetingData.Empty;
            return float.NegativeInfinity;
        }

        targeting = new TargetingData(
            chosen.Transform.position,
            chosen.gridPosition,
            true,
            chosen
        );
        return 1f;
    }

    public override bool CanExecute(IInteractableElement caster, TargetingData? targetingData, ref object cache) =>
        targetingData.HasValue &&
        targetingData.Value.selectedTarget is GridCharacter character &&
        character.Health.IsAlive;

    public override ICommand CreateCommand(IInteractableElement caster, TargetingData? targetingData, ref object cache) =>
        new SlimyCurseCommand(
            caster,
            (GridCharacter)targetingData.Value.selectedTarget,
            _cursePassive
        );
}
