using System;
using System.Collections.Generic;
using UnityEngine;

public class MoveCommand : ICommand
{
    private readonly GridCharacter _caster;
    private readonly List<Vector3> _path;
    private readonly Func<Vector3Int, int> _costGetter;
    private Vector3Int _oldPosition;

    public MoveCommand(GridCharacter caster, List<Vector3> path, Func<Vector3Int, int> costGetter = null)
    {
        _caster = caster;
        _path = path;
        _costGetter = costGetter;
        _oldPosition = caster.gridPosition;
    }

    public async Awaitable ExecuteAsync()
    {
        await _caster.MoveTo(_path, _costGetter, _caster.destroyCancellationToken);
    }

    public void Undo()
    {
        _caster.gridPosition = _oldPosition;
        _caster.transform.position = _caster.activeTilemap.CellToWorld(_oldPosition);
    }
}
