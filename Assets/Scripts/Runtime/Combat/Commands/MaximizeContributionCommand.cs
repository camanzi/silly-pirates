using UnityEngine;

/// <summary>
/// Instantly awakens a sleeping equipment: fills its points up to <see cref="IAwakable.MaxAwakeningPoints"/>.
/// </summary>
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
        _target.AddAwakeningPoints(_target.MaxAwakeningPoints - _target.CurrentAwakeningPoints);

        await Awaitable.NextFrameAsync();
    }

    public void Undo()
    {
        if (_caster is ITurnAgent turnAgent)
            turnAgent.RemainingActionPoints += _apCost;

        // Taking the added points back drops the equipment below the threshold, and RemoveAwakeningPoints
        // puts it back to sleep.
        int delta = _target.CurrentAwakeningPoints - _pointsBeforeAdd;
        if (delta > 0)
            _target.RemoveAwakeningPoints(delta);
    }
}
