using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

public class DynamicElementalResistanceCommand : ICommand
{
    private readonly HostileCharacter _caster;
    private readonly ElementalShieldController _shield;
    private readonly DamageType _newElement;
    private readonly AbilityBase _ability;
    private readonly AbilityExecutionCueEventChannel _cameraCueChannel;
    private readonly CameraDirectorStateSO _cameraDirectorState;

    public DynamicElementalResistanceCommand(HostileCharacter caster, ElementalShieldController shield,
        DamageType newElement,
        AbilityBase ability = null,
        AbilityExecutionCueEventChannel cameraCueChannel = null,
        CameraDirectorStateSO cameraDirectorState = null)
    {
        _caster = caster;
        _shield = shield;
        _newElement = newElement;
        _ability = ability;
        _cameraCueChannel = cameraCueChannel;
        _cameraDirectorState = cameraDirectorState;
    }

    public async Awaitable ExecuteAsync()
    {
        Transform shieldTransform = _shield.transform;

        if (_cameraDirectorState != null && _cameraCueChannel != null)
        {
            var cue = new AbilityExecutionCue(_ability, _caster, new List<ITargettable> { _caster }, null, shieldTransform.position)
            {
                CueTypeOverride = CameraCueType.FocusTarget,
                ProfileOverride = _ability != null ? _ability.CameraCueProfile : null
            };
            await _cameraDirectorState.RaiseCueAndWaitAsync(_cameraCueChannel, cue);
        }

        // Carica e scarica: lo scudo si contrae, poi sbotta nel nuovo elemento.
        Vector3 originalScale = shieldTransform.localScale;
        await Tween.Scale(shieldTransform, originalScale * 0.75f, 0.18f, Ease.InQuad);

        _shield.SetElement(_newElement);

        await Tween.Scale(shieldTransform, originalScale * 1.2f, 0.12f, Ease.OutBack);
        await Tween.Scale(shieldTransform, originalScale, 0.15f, Ease.OutQuad);

        await Awaitable.NextFrameAsync();
    }

    public void Undo() { }
}
