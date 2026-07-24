using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

public class SlimyCurseCommand : ICommand
{
    private readonly IInteractableElement _caster;
    private readonly GridCharacter _target;
    private readonly SlimyCursePassiveSO _curseSO;
    private readonly AbilityBase _ability;
    private readonly AbilityExecutionCueEventChannel _cameraCueChannel;
    private readonly CameraDirectorStateSO _cameraDirectorState;

    public SlimyCurseCommand(IInteractableElement caster, GridCharacter target, SlimyCursePassiveSO curseSO,
        AbilityBase ability = null,
        AbilityExecutionCueEventChannel cameraCueChannel = null,
        CameraDirectorStateSO cameraDirectorState = null)
    {
        _caster = caster;
        _target = target;
        _curseSO = curseSO;
        _ability = ability;
        _cameraCueChannel = cameraCueChannel;
        _cameraDirectorState = cameraDirectorState;
    }

    public async Awaitable ExecuteAsync()
    {
        var t = _caster.Transform;
        Vector3 origin = t.position;
        const float radius = 1f;

        await Tween.Position(t, origin + new Vector3(radius, 0f, 0f), 0.1f, Ease.OutQuad);

        await Tween.Custom(0f, Mathf.PI * 4f, 1.5f, ease: Ease.Linear,
            onValueChange: angle => t.position = origin + new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f));

        await Tween.Position(t, origin, 0.2f, Ease.OutQuad);

        for (int i = 0; i < 2; i++)
        {
            await Tween.Position(t, origin + Vector3.up * radius, 0.12f, Ease.OutQuad);
            await Tween.Position(t, origin, 0.12f, Ease.InQuad);
        }

        if (_cameraDirectorState != null && _cameraCueChannel != null)
        {
            var cue = new AbilityExecutionCue(_ability, _caster, new List<ITargettable> { _target }, null, _target.Transform.position)
            {
                CueTypeOverride = CameraCueType.FocusTarget,
                ProfileOverride = _ability != null ? _ability.CameraCueProfile : null
            };
            await _cameraDirectorState.RaiseCueAndWaitAsync(_cameraCueChannel, cue);
        }

        _target.PassiveAbilityController.AddPassive(Object.Instantiate(_curseSO));
        await Awaitable.NextFrameAsync();
    }

    public void Undo() { }
}
