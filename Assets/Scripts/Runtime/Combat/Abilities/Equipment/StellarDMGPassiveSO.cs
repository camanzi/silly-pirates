using UnityEngine;

[CreateAssetMenu(fileName = "Stellar DMG Passive", menuName = "Abilities/Equipment/Stellar DMG Passive")]
public class StellarDMGPassiveSO : PassiveAbilitySO, IDMGTypeModifier
{
    private PassiveAbilityController _controller;
    private ITurnAgent _caster;
    private TurnAgentEventChannel _onAnyTurnEnded;

    public void Initialize(ITurnAgent caster, TurnAgentEventChannel onAnyTurnEnded)
    {
        _caster = caster;
        _onAnyTurnEnded = onAnyTurnEnded;
    }

    public override void OnEquip(PassiveAbilityController controller)
    {
        _controller = controller;
        if (controller.TryGetComponent<OffensiveEquipment>(out var eq))
            eq.AddDMGTypeModifier(this);
        _onAnyTurnEnded.OnEventRaised += OnTurnEnded;
    }

    public override void OnUnequip(PassiveAbilityController controller)
    {
        _onAnyTurnEnded.OnEventRaised -= OnTurnEnded;
        if (controller.TryGetComponent<OffensiveEquipment>(out var eq))
            eq.RemoveDMGTypeModifier(this);
        _controller = null;
    }

    DamageType IDMGTypeModifier.GetDMGTypeOverride() => DamageType.Stellar;

    private void OnTurnEnded(ITurnAgent agent)
    {
        if (agent != _caster) return;
        _controller.RemovePassive(this);
    }
}
