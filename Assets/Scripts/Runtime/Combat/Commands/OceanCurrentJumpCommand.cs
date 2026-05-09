using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

public class OceanCurrentJumpCommand : ICommand
{
    private readonly GridCharacter _caster;
    private readonly List<Vector3> _pathToBorder;
    private readonly Vector3Int _entryCell;
    private readonly Vector3Int _exitCurrentCell; // nearest other current to exit cell
    private readonly Vector3Int _exitCell;
    private readonly float _jumpPeakHeight;
    private readonly float _diveDuration;
    private readonly float _exitDuration;
    private readonly System.Action _onUsed;

    public OceanCurrentJumpCommand(
        GridCharacter caster,
        List<Vector3> pathToBorder,
        Vector3Int entryCell,
        Vector3Int exitCurrentCell,
        Vector3Int exitCell,
        System.Action onUsed,
        float jumpPeakHeight,
        float diveDuration,
        float exitDuration)
    {
        _caster = caster;
        _pathToBorder = pathToBorder;
        _entryCell = entryCell;
        _exitCurrentCell = exitCurrentCell;
        _exitCell = exitCell;
        _onUsed = onUsed;
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
        Vector3 diveControl = (borderWorld + entryUnder) / 2f + Vector3.up * 0.5f;

        await Tween.Custom(0f, 1f, duration: _diveDuration, ease: Ease.InQuad, onValueChange: t =>
        {
            _caster.transform.position = MathUtils.EvaluateBezierPoint(t, borderWorld, diveControl, entryUnder);
        });

        Vector3 exitCurrentWorld = tilemap.GetCellCenterWorld(_exitCurrentCell);
        Vector3 exitCurrentUnder = exitCurrentWorld + Vector3.down * 2f;
        Vector3 exitWorld = tilemap.GetCellCenterWorld(_exitCell);
        Vector3 exitControl = (exitCurrentUnder + exitWorld) / 2f + Vector3.up * _jumpPeakHeight;

        await Tween.Custom(0f, 1f, duration: _exitDuration, ease: Ease.OutQuad, onValueChange: t =>
        {
            _caster.transform.position = MathUtils.EvaluateBezierPoint(t, exitCurrentUnder, exitControl, exitWorld);
        });

        _caster.gridPosition = _exitCell;

        _onUsed?.Invoke();
    }

    public void Undo() { }
}
