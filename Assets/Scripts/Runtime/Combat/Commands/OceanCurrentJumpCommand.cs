using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

public class OceanCurrentJumpCommand : ICommand
{
    private readonly GridCharacter _caster;
    private readonly List<Vector3> _pathToBorder;
    private readonly Vector3Int _entryCell;
    private readonly Vector3Int _exitCurrentCell;
    private readonly Vector3Int _exitCell;
    private readonly float _jumpPeakHeight;
    private readonly float _divePeakHeight;
    private readonly float _diveDuration;
    private readonly float _exitDuration;
    private readonly System.Action _onUsed;
    private Vector3 _tweenStart, _tweenMid, _tweenEnd;

    public OceanCurrentJumpCommand(
        GridCharacter caster,
        List<Vector3> pathToBorder,
        Vector3Int entryCell,
        Vector3Int exitCurrentCell,
        Vector3Int exitCell,
        System.Action onUsed,
        float jumpPeakHeight,
        float divePeakHeight,
        float diveDuration,
        float exitDuration)
    {
        _caster = caster;
        _pathToBorder = pathToBorder;
        _entryCell = entryCell;
        _exitCurrentCell = exitCurrentCell;
        _exitCell = exitCell;
        _onUsed = onUsed;
        _divePeakHeight = divePeakHeight;
        _jumpPeakHeight = jumpPeakHeight;
        _diveDuration = diveDuration;
        _exitDuration = exitDuration;
    }

    public async Awaitable ExecuteAsync()
    {
        var tilemap = _caster.activeTilemap;

        if (_pathToBorder.Count > 0)
            await _caster.MoveTo(_pathToBorder, _caster.destroyCancellationToken);

        Vector3 borderWorld = tilemap.GetCellCenterWorld(_caster.gridPosition);
        Vector3 entryWorld = tilemap.GetCellCenterWorld(_entryCell);
        Vector3 entryUnder = entryWorld + Vector3.down * 2f;
        Vector3 diveControl = (borderWorld + entryUnder) / 2f + Vector3.up * _divePeakHeight;

        _tweenStart = borderWorld;
        _tweenMid = diveControl;
        _tweenEnd = entryUnder;

        await Tween.Custom(this, 0f, 1f, duration: _diveDuration, ease: Ease.InQuad,
            onValueChange: static (cmd, t) =>
                cmd._caster.transform.position = MathUtils.EvaluateBezierPoint(t, cmd._tweenStart, cmd._tweenMid, cmd._tweenEnd));

        Vector3 exitCurrentWorld = tilemap.GetCellCenterWorld(_exitCurrentCell);
        Vector3 exitCurrentUnder = exitCurrentWorld + Vector3.down * 2f;
        Vector3 exitWorld = tilemap.GetCellCenterWorld(_exitCell);
        Vector3 exitControl = (exitCurrentUnder + exitWorld) / 2f + Vector3.up * _jumpPeakHeight;

        _tweenStart = exitCurrentUnder; 
        _tweenMid = exitControl;
        _tweenEnd = exitWorld;
        
        await Tween.Custom(this, 0f, 1f, duration: _exitDuration, ease: Ease.OutQuad,
            onValueChange: static (cmd, t) =>
                cmd._caster.transform.position = MathUtils.EvaluateBezierPoint(t, cmd._tweenStart, cmd._tweenMid, cmd._tweenEnd));

        _caster.gridPosition = _exitCell;

        _onUsed?.Invoke();
    }

    public void Undo() { }
}
