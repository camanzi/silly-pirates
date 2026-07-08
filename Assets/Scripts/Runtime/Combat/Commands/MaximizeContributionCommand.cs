using UnityEngine;

public class MaximizeContributionCommand : ICommand
{
    private readonly IInteractableElement _caster;
    private readonly ShipEquipment _target;
    private readonly int _apCost;

    private int _pointsBeforeAdd;

    public MaximizeContributionCommand(IInteractableElement caster, ShipEquipment target, int apCost)
    {
        _caster = caster;
        _target = target;
        _apCost = apCost;
    }

    public async Awaitable ExecuteAsync()
    {
        if (_caster is ITurnAgent turnAgent)
            turnAgent.RemainingActionPoints -= _apCost;

        _pointsBeforeAdd = _target.CurrentAwakeningPoints;
        _target.AddAwakeningPoints(_target.OvercapLimit - _target.CurrentAwakeningPoints);

        ApplyOvercapPassive();

        await Awaitable.NextFrameAsync();
    }

    public void Undo()
    {
        if (_caster is ITurnAgent turnAgent)
            turnAgent.RemainingActionPoints += _apCost;

        int delta = _target.CurrentAwakeningPoints - _pointsBeforeAdd;
        if (delta > 0)
            _target.RemoveAwakeningPoints(delta);
    }

    private void ApplyOvercapPassive()
    {
        var template = _target.StatsConfig?.OvercapPassiveTemplate;
        if (template == null || _target.PassiveAbilityController == null) return;

        int extra = Mathf.Max(0, _target.CurrentAwakeningPoints - _target.MaxAwakeningPoints);
        int bonus = _target.StatsConfig.GetOvercapBonus(extra);

        _target.PassiveAbilityController.RemovePassiveOfType<IOvercapPassive>();
        var instance = Object.Instantiate(template);
        (instance as IOvercapPassive)?.Initialize(bonus);
        _target.PassiveAbilityController.AddPassive(instance);
    }
}
