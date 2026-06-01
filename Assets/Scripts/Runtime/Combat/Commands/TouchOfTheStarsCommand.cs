using UnityEngine;

public class TouchOfTheStarsCommand : ICommand
{
    private readonly IInteractableElement _caster;
    private readonly int _apCost;
    private readonly TurnAgentEventChannel _turnEndedChannel;
    private AwakeningStackModifier _modifier;

    public TouchOfTheStarsCommand(IInteractableElement caster, int apCost, TurnAgentEventChannel turnEndedChannel)
    {
        _caster = caster;
        _apCost = apCost;
        _turnEndedChannel = turnEndedChannel;
    }

    public async Awaitable ExecuteAsync()
    {
        if (_caster is IAwakeningModifierHolder holder && _caster is ITurnAgent agent)
        {
            _modifier = new AwakeningStackModifier(holder, _turnEndedChannel, agent);
            holder.AddAwakeningModifier(_modifier);
        }
        await Awaitable.NextFrameAsync();
    }

    public void Undo()
    {
        _modifier?.Remove();
        if (_caster is ITurnAgent agent)
            agent.RemainingActionPoints += _apCost;
    }

    private sealed class AwakeningStackModifier : IAwakeningModifier, IAwakeningContributionEffect
    {
        private readonly IAwakeningModifierHolder _holder;
        private readonly TurnAgentEventChannel _channel;
        private readonly ITurnAgent _owner;
        private bool _active = true;

        public AwakeningStackModifier(IAwakeningModifierHolder holder, TurnAgentEventChannel channel, ITurnAgent owner)
        {
            _holder = holder;
            _channel = channel;
            _owner = owner;
            _channel.OnEventRaised += OnTurnEnded;
        }

        public int GetAwakeningBonus() => 1;

        public void OnContribution(IAwakable awakable)
        {
            if (awakable is OffensiveEquipment equipment)
                equipment.AddDMGTypeModifier(new StellarDMGModifier(equipment, _channel, _owner));
        }

        private void OnTurnEnded(ITurnAgent agent)
        {
            if (agent != _owner) return;
            Remove();
        }

        public void Remove()
        {
            if (!_active) return;
            _active = false;
            _holder.RemoveAwakeningModifier(this);
            _channel.OnEventRaised -= OnTurnEnded;
        }
    }

}
