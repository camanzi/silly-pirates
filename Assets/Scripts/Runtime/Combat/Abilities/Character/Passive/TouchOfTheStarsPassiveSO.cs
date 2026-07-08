using UnityEngine;

[CreateAssetMenu(fileName = "Touch Of The Stars Passive", menuName = "Abilities/Character/Passives/Touch Of The Stars Passive")]
public class TouchOfTheStarsPassiveSO : PassiveAbilitySO, IAwakeningModifier
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

    private void OnTurnEnded(ITurnAgent agent)
    {
        if (agent != _caster) return;
        _controller.RemovePassive(this);
    }
}
