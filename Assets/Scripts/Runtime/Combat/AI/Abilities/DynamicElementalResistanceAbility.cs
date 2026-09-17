using UnityEngine;

/// <summary>
/// Changes the element of the caster's elemental shield, picking the one the player has used least.
/// Gated on the part through <see cref="EnemyAbilityBase"/>'s _requiredPart: with the shield broken the
/// ability drops out of the candidate set automatically.
/// </summary>
[CreateAssetMenu(fileName = "DynamicElementalResistance Ability", menuName = "Abilities/Enemy/Dynamic Elemental Resistance")]
public class DynamicElementalResistanceAbility : EnemyAbilityBase, IDefensiveAbility
{
    [Header("Dynamic Elemental Resistance configs")]
    [Tooltip("Turns to wait between one use and the next.")]
    [SerializeField] private int _cooldownTurns = 1;

    [Header("Camera Direction")]
    [SerializeField] private AbilityExecutionCueEventChannel _cameraCueChannel;
    [SerializeField] private CameraDirectorStateSO _cameraDirectorState;

    protected override bool MeetsPreconditions(AIContext context)
    {
        if (!base.MeetsPreconditions(context)) return false;
        return !context.Caster.AbilityCooldowns.TryGetValue(this, out int cd) || cd == 0;
    }

    protected override float ComputeScore(AIContext context, out TargetingData targeting)
    {
        var shield = GetShield(context.Caster);
        if (shield == null || !shield.IsActive)
        {
            targeting = TargetingData.Empty;
            return float.NegativeInfinity;
        }

        // The target is the caster itself: there is no "self target" convention in this project, buff
        // abilities fill targeting with the recipient all the same.
        targeting = new TargetingData(context.Caster.Transform.position, default, true, context.Caster);
        return 1f;
    }

    public override bool CanExecute(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        var shield = GetShield(caster);
        return shield != null && shield.IsActive;
    }

    public override ICommand CreateCommand(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        var hostile = (HostileCharacter)caster;
        var shield = GetShield(hostile);
        if (shield == null) return null;

        // +1 as in SpeedBoostAbility: HostileCharacter.OnStartingTurn decrements on every turn of the
        // owner, including the one the ability is used on.
        hostile.AbilityCooldowns[this] = _cooldownTurns + 1;

        return new DynamicElementalResistanceCommand(hostile, shield, shield.PickDenialElement(), this,
            _cameraCueChannel, _cameraDirectorState);
    }

    private ElementalShieldController GetShield(IInteractableElement caster)
    {
        Transform partTransform = GetRequiredPartTransform(caster);
        return partTransform != null ? partTransform.GetComponent<ElementalShieldController>() : null;
    }
}
