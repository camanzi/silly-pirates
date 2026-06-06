using System.Collections.Generic;
using UnityEngine;

public class TelegraphCommand : ICommand
{
    private readonly List<Vector3Int> _cells;
    private readonly CellEffectEventChannel _channel;
    private readonly Material _material;
    private readonly string _key;

    public TelegraphCommand(List<Vector3Int> cells, CellEffectEventChannel channel, Material material, string key)
    {
        _cells = cells;
        _channel = channel;
        _material = material;
        _key = key;
    }

    public async Awaitable ExecuteAsync()
    {
        _channel?.RaiseEvent(new CellEffectPayload
        {
            Key = _key,
            Cells = _cells,
            Material = _material
        });
        await Awaitable.NextFrameAsync();
    }

    public void Undo()
    {
        _channel?.RaiseEvent(new CellEffectPayload { Key = _key, Cells = null });
    }
}
