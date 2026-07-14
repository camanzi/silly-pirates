using System;
using UnityEngine;

[CreateAssetMenu(fileName = "Touch Of The Stars Passive", menuName = "Abilities/Character/Passives/Touch Of The Stars Passive")]
public class TouchOfTheStarsPassiveSO : PassiveAbilitySO, IAwakeningModifier, IPassiveStateNotifier, IStackCountProvider, IStackablePassive
{
    private PassiveAbilityController _controller;
    private ITurnAgent _caster;
    private TurnAgentEventChannel _onAnyTurnEnded;

    private int _stacks;

    public int CurrentStacks => _stacks;
    public int MaxStacks => int.MaxValue;

    public event Action OnStateUpdated;

    event Action IPassiveStateNotifier.OnStateChanged
    {
        add => OnStateUpdated += value;
        remove => OnStateUpdated -= value;
    }

    public void Initialize(ITurnAgent caster, TurnAgentEventChannel onAnyTurnEnded)
    {
        _caster = caster;
        _onAnyTurnEnded = onAnyTurnEnded;
    }

    public override void OnEquip(PassiveAbilityController controller)
    {
        _stacks = 1;
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

    int IAwakeningModifier.GetAwakeningBonus() => _stacks;

    void IStackablePassive.OnReapplied(PassiveAbilityController controller) => AddStack();

    public void AddStack()
    {
        _stacks++;
        OnStateUpdated?.Invoke();
    }

    public void RemoveStack()
    {
        if (_stacks > 1) _stacks--;
        OnStateUpdated?.Invoke();
    }

    private void OnTurnEnded(ITurnAgent agent)
    {
        if (agent != _caster) return;
        _controller.RemovePassive(this);
    }
}
