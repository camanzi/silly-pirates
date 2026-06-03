using UnityEngine;

public class CaliberExpansionCommand : ICommand
{
    private readonly PassiveAbilityController _passiveController;
    private readonly ITurnAgent _caster;
    private readonly CaliberExpansionPassiveSO _passiveSO;
    private readonly TurnAgentEventChannel _onAnyTurnEnded;
    private CaliberExpansionPassiveSO _passiveInstance;

    public CaliberExpansionCommand(PassiveAbilityController passiveController, ITurnAgent caster, CaliberExpansionPassiveSO passiveSO, TurnAgentEventChannel onAnyTurnEnded)
    {
        _passiveController = passiveController;
        _caster = caster;
        _passiveSO = passiveSO;
        _onAnyTurnEnded = onAnyTurnEnded;
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
    }
}
