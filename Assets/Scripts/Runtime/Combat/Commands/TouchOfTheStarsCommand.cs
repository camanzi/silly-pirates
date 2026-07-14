using UnityEngine;

public class TouchOfTheStarsCommand : ICommand
{
    private readonly PassiveAbilityController _passiveController;
    private readonly ITurnAgent _caster;
    private readonly TouchOfTheStarsPassiveSO _passiveSO;
    private readonly TurnAgentEventChannel _onAnyTurnEnded;
    private readonly int _apCost;
    private TouchOfTheStarsPassiveSO _passiveInstance;
    private bool _createdInstance;

    public TouchOfTheStarsCommand(PassiveAbilityController passiveController, ITurnAgent caster, TouchOfTheStarsPassiveSO passiveSO, TurnAgentEventChannel onAnyTurnEnded, int apCost)
    {
        _passiveController = passiveController;
        _caster = caster;
        _passiveSO = passiveSO;
        _onAnyTurnEnded = onAnyTurnEnded;
        _apCost = apCost;
    }

    public async Awaitable ExecuteAsync()
    {
        var candidate = Object.Instantiate(_passiveSO);
        candidate.Initialize(_caster, _onAnyTurnEnded);
        var result = _passiveController.AddPassive(candidate);
        _createdInstance = ReferenceEquals(result, candidate);
        _passiveInstance = (TouchOfTheStarsPassiveSO)result;
        await Awaitable.NextFrameAsync();
    }

    public void Undo()
    {
        if (_passiveInstance == null) return;
        if (_createdInstance) _passiveController.RemovePassive(_passiveInstance);
        else _passiveInstance.RemoveStack();
        _passiveInstance = null;
        _caster.RemainingActionPoints += _apCost;
    }
}
