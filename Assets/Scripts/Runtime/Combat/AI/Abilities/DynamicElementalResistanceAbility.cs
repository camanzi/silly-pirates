using UnityEngine;

/// <summary>
/// Cambia l'elemento dello scudo elementale del caster, scegliendo quello che il giocatore ha usato meno.
/// Gate sulla parte tramite _requiredPart di <see cref="EnemyAbilityBase"/>: a scudo rotto l'abilita'
/// esce automaticamente dalle candidate.
/// </summary>
[CreateAssetMenu(fileName = "DynamicElementalResistance Ability", menuName = "Abilities/Enemy/Dynamic Elemental Resistance")]
public class DynamicElementalResistanceAbility : EnemyAbilityBase
{
    [Header("Dynamic Elemental Resistance configs")]
    [Tooltip("Turni di attesa fra un uso e il successivo.")]
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

        // Il bersaglio e' il caster stesso: non c'e' una convenzione di "self target" in questo progetto,
        // le abilita' di buff riempiono comunque targeting con il destinatario.
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

        // +1 come in SpeedBoostAbility: HostileCharacter.OnStartingTurn decrementa a ogni turno dell'owner,
        // incluso quello in cui l'abilita' viene usata.
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
