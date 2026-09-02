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
            // Lo shake scrive localPosition della parte satellite: stesso canale dell'idle in loop.
            // _caster è la root del personaggio (il comando la riceve così, non un HostileCharacter).
            CharacterLifecycleAnimator lifecycleAnimator = _caster.GetComponent<CharacterLifecycleAnimator>();
            lifecycleAnimator?.SuspendLoop();

            await Tween.Scale(_partTransform, partScale * 1.2f, 0.5f, Ease.InOutQuad);
            _state.Extra[MultiStepAbilityStepSO.ShakeTweenKey] =
                Tween.ShakeLocalPosition(_partTransform, strength: new Vector3(0.5f, 0f, 0f), duration: 0.12f, cycles: -1);
        }

        await Awaitable.NextFrameAsync();
    }

    /// <summary>
    /// NON è il teardown di questo shake: <c>ICommand.Undo()</c> non viene invocato da nessuna parte in
    /// questo progetto. Lo shake lo chiudono <see cref="SlimeBombingCommand"/> (percorso normale) e
    /// <see cref="ThreatenAreaStepSO.OnRolledBack"/> (interruzione), entrambi via
    /// <see cref="MultiStepAbilityStepSO.EndPartShake"/> — la stessa chiamata usata qui, così che se un
    /// giorno l'undo venisse cablato non ci sia un secondo teardown divergente da tenere allineato.
    /// </summary>
    public void Undo()
    {
        MultiStepAbilityStepSO.EndPartShake(_state, _caster.GetComponent<CharacterLifecycleAnimator>());

        if (_partTransform != null && _state.Extra.TryGetValue(MultiStepAbilityStepSO.PartOriginalScaleKey, out var psObj) && psObj is Vector3 ps && ps != Vector3.zero)
            _partTransform.localScale = ps;
        _channel?.RaiseEvent(new CellEffectPayload { Key = _key, Cells = null });
    }
}
