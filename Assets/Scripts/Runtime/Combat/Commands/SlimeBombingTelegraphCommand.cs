using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

public class SlimeBombingTelegraphCommand : ICommand
{
    private readonly List<Vector3Int> _cells;
    private readonly CellEffectEventChannel _channel;
    private readonly Material _material;
    private readonly string _key;
    private readonly SlimeBombingAbility.SlimeBombingState _state;
    private readonly HostileCharacter _caster;

    public SlimeBombingTelegraphCommand(
        List<Vector3Int> cells, CellEffectEventChannel channel, Material material, string key,
        SlimeBombingAbility.SlimeBombingState state, HostileCharacter caster)
    {
        _cells   = cells;
        _channel = channel;
        _material = material;
        _key     = key;
        _state   = state;
        _caster  = caster;
    }

    public async Awaitable ExecuteAsync()
    {
        _channel?.RaiseEvent(new CellEffectPayload { Key = _key, Cells = _cells, Material = _material });

        _state.CasterOriginalScale = _caster.Transform.localScale;
        _state.PartOriginalScale   = _state.PartTransform != null ? _state.PartTransform.localScale : Vector3.one;

        if (_state.PartTransform != null)
        {
            await Tween.Scale(_state.PartTransform, _state.PartOriginalScale * 1.2f, 0.5f, Ease.InOutQuad);
            _state.ActiveShakeTween = Tween.ShakeLocalPosition(_state.PartTransform, strength: new Vector3(0.5f, 0.0f, 0.0f), duration: 0.12f, cycles: -1);
        }

        await Awaitable.NextFrameAsync();
    }

    public void Undo()
    {
        if (_state.ActiveShakeTween.isAlive) _state.ActiveShakeTween.Stop();

        if (_state.PartTransform != null && _state.PartOriginalScale != Vector3.zero)
            _state.PartTransform.localScale = _state.PartOriginalScale;

        _channel?.RaiseEvent(new CellEffectPayload { Key = _key, Cells = null });
    }
}
