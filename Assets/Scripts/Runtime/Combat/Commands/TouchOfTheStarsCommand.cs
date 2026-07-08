using UnityEngine;

public class TouchOfTheStarsCommand : ICommand
{
    private readonly PassiveAbilityController _passiveController;
    private readonly ITurnAgent _caster;
    private readonly TouchOfTheStarsPassiveSO _passiveSO;
    private readonly TurnAgentEventChannel _onAnyTurnEnded;
    private readonly int _apCost;
    private TouchOfTheStarsPassiveSO _passiveInstance;

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
        _passiveInstance = Object.Instantiate(_passiveSO);
        _passiveInstance.Initialize(_caster, _onAnyTurnEnded);
        _passiveController.AddPassive(_passiveInstance);
        await Awaitable.NextFrameAsync();
    }

    public void Undo()
    {
        if (_passiveInstance == null) return;
        _passiveController.RemovePassive(_passiveInstance);
        _passiveInstance = null;
        _caster.RemainingActionPoints += _apCost;
    }
}
