using System.Collections.Generic;
using UnityEngine;

public class MoveCommand : ICommand
{
    private readonly GridCharacter _caster;
    private readonly List<Vector3Int> _path;
    private Vector3Int _oldPosition;

    public MoveCommand(GridCharacter caster, List<Vector3Int> path)
    {
        _caster = caster;
        _path = path;
        _oldPosition = caster.gridPosition;
    }

    public async Awaitable ExecuteAsync()
    {
        await _caster.MoveTo(_path, _caster.destroyCancellationToken);
    }

    public void Undo()
    {
        _caster.gridPosition = _oldPosition;
        _caster.transform.position = _caster.activeTilemap.CellToWorld(_oldPosition);
    }
}
