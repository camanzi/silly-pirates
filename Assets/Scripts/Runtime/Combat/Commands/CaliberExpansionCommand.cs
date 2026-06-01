using UnityEngine;

public class CaliberExpansionCommand : ICommand
{
    private readonly OffensiveEquipment _targetEquipment;
    private readonly ITurnAgent _caster;
    private readonly int _critDMGBonus;
    private readonly TurnAgentEventChannel _turnEndedChannel;
    private CritDMGBonusModifier _modifier;
    private bool _applied;

    public CaliberExpansionCommand(OffensiveEquipment targetEquipment, ITurnAgent caster, int critDMGBonus, TurnAgentEventChannel turnEndedChannel)
    {
        _targetEquipment = targetEquipment;
        _caster = caster;
        _critDMGBonus = critDMGBonus;
        _turnEndedChannel = turnEndedChannel;
    }

    public async Awaitable ExecuteAsync()
    {
        _modifier = new CritDMGBonusModifier(_critDMGBonus);
        _turnEndedChannel.OnEventRaised += OnTurnEnded;
        _targetEquipment.AddCritDMGModifier(_modifier);
        _applied = true;
        await Awaitable.NextFrameAsync();
    }

    public void Undo()
    {
        if (!_applied) return;
        _targetEquipment.RemoveCritDMGModifier(_modifier);
        _turnEndedChannel.OnEventRaised -= OnTurnEnded;
        _applied = false;
    }

    private void OnTurnEnded(ITurnAgent agent)
    {
        if (agent != _caster) return;
        _targetEquipment.RemoveCritDMGModifier(_modifier);
        _turnEndedChannel.OnEventRaised -= OnTurnEnded;
        _applied = false;
    }

    private sealed class CritDMGBonusModifier : ICritDMGModifier
    {
        private readonly int _bonus;
        public CritDMGBonusModifier(int bonus) => _bonus = bonus;
        public int GetCritDMGBonus() => _bonus;
    }
}
