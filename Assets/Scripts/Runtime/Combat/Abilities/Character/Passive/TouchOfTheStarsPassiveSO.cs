using UnityEngine;

[CreateAssetMenu(fileName = "Touch Of The Stars Passive", menuName = "Abilities/Character/Passives/Touch Of The Stars Passive")]
public class TouchOfTheStarsPassiveSO : PassiveAbilitySO, IAwakeningModifier, IAwakeningContributionEffect
{
    private PassiveAbilityController _controller;
    private ITurnAgent _caster;
    private TurnAgentEventChannel _onAnyTurnEnded;
    private StellarDMGPassiveSO _stellarPassiveSO;

    public void Initialize(ITurnAgent caster, TurnAgentEventChannel onAnyTurnEnded, StellarDMGPassiveSO stellarPassiveSO)
    {
        _caster = caster;
        _onAnyTurnEnded = onAnyTurnEnded;
        _stellarPassiveSO = stellarPassiveSO;
    }

    public override void OnEquip(PassiveAbilityController controller)
    {
        _controller = controller;
        if (controller.TryGetComponent<IAwakeningModifierHolder>(out var holder))
            holder.AddAwakeningModifier(this);
        _onAnyTurnEnded.OnEventRaised += OnTurnEnded;
    }

    public override void OnUnequip(PassiveAbilityController controller)
    {
        _onAnyTurnEnded.OnEventRaised -= OnTurnEnded;
        if (controller.TryGetComponent<IAwakeningModifierHolder>(out var holder))
            holder.RemoveAwakeningModifier(this);
        _controller = null;
    }

    int IAwakeningModifier.GetAwakeningBonus() => 1;

    void IAwakeningContributionEffect.OnContribution(IAwakable awakable)
    {
        if (awakable is not ShipEquipment equipment) return;
        var ctrl = equipment.PassiveAbilityController;
        if (ctrl == null) return;
        if (ctrl.HasPassive<StellarDMGPassiveSO>()) return;
        var passive = Object.Instantiate(_stellarPassiveSO);
        passive.Initialize(_caster, _onAnyTurnEnded);
        ctrl.AddPassive(passive);
    }

    private void OnTurnEnded(ITurnAgent agent)
    {
        if (agent != _caster) return;
        _controller.RemovePassive(this);
    }
}
