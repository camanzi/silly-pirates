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

        await Awaitable.NextFrameAsync();
    }

    public void Undo()
    {
        if (_caster is ITurnAgent turnAgent)
            turnAgent.RemainingActionPoints += _apCost;

        // Rimuovere i punti riporta l'overcap al valore precedente: il passivo lo
        // aggiorna (o rimuove) ShipEquipment.RefreshOvercapPassive.
        int delta = _target.CurrentAwakeningPoints - _pointsBeforeAdd;
        if (delta > 0)
            _target.RemoveAwakeningPoints(delta);
    }
}
