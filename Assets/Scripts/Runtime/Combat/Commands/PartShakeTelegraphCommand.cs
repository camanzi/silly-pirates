using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

public class PartShakeTelegraphCommand : ICommand
{
    private readonly List<Vector3Int> _cells;
    private readonly CellEffectEventChannel _channel;
    private readonly Material _material;
    private readonly string _key;
    private readonly Transform _caster;
    private readonly Transform _partTransform;
    private readonly StepState _state;

    public PartShakeTelegraphCommand(List<Vector3Int> cells, CellEffectEventChannel channel, Material material,
        string key, Transform caster, Transform partTransform, StepState state)
    { _cells = cells; _channel = channel; _material = material; _key = key; _caster = caster; _partTransform = partTransform; _state = state; }

    public async Awaitable ExecuteAsync()
    {
        _channel?.RaiseEvent(new CellEffectPayload { Key = _key, Cells = _cells, Material = _material });

        Vector3 casterScale = _caster.localScale;
        Vector3 partScale = _partTransform != null ? _partTransform.localScale : Vector3.one;
        _state.Extra[MultiStepAbilityStepSO.CasterOriginalScaleKey] = casterScale;
        _state.Extra[MultiStepAbilityStepSO.PartOriginalScaleKey] = partScale;

        if (_partTransform != null)
        {
            // The shake writes the satellite part's localPosition: the same channel as the looping idle.
            // _caster is the character's root (that is how the command receives it, not a HostileCharacter).
            CharacterLifecycleAnimator lifecycleAnimator = _caster.GetComponent<CharacterLifecycleAnimator>();
            lifecycleAnimator?.SuspendLoop();

            await Tween.Scale(_partTransform, partScale * 1.2f, 0.5f, Ease.InOutQuad);
            _state.Extra[MultiStepAbilityStepSO.ShakeTweenKey] =
                Tween.ShakeLocalPosition(_partTransform, strength: new Vector3(0.5f, 0f, 0f), duration: 0.12f, cycles: -1);
        }

        await Awaitable.NextFrameAsync();
    }

    /// <summary>
    /// This is NOT this shake's teardown: <c>ICommand.Undo()</c> is invoked nowhere in this project. The
    /// shake is closed by <see cref="SlimeBombingCommand"/> (the normal path) and
    /// <see cref="ThreatenAreaStepSO.OnRolledBack"/> (an interruption), both through
    /// <see cref="MultiStepAbilityStepSO.EndPartShake"/> — the same call used here, so that if undo is
    /// ever wired up one day there is no second, divergent teardown to keep in sync.
    /// </summary>
    public void Undo()
    {
        MultiStepAbilityStepSO.EndPartShake(_state, _caster.GetComponent<CharacterLifecycleAnimator>());

        if (_partTransform != null && _state.Extra.TryGetValue(MultiStepAbilityStepSO.PartOriginalScaleKey, out var psObj) && psObj is Vector3 ps && ps != Vector3.zero)
            _partTransform.localScale = ps;
        _channel?.RaiseEvent(new CellEffectPayload { Key = _key, Cells = null });
    }
}
