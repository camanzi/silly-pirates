using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "Threaten Area Step", menuName = "Abilities/Enemy/Steps/Threaten Area")]
public class ThreatenAreaStepSO : MultiStepAbilityStepSO
{
    [Header("Area Config")]
    [SerializeField] private ShapeType _shapeType = ShapeType.Circle;
    [SerializeField] private int _radius = 1;
    [SerializeField] private int _areaCount = 2;
    [SerializeField] private TargetSelectionStrategySO _selectionStrategy;

    [Header("Telegraph Visual")]
    [SerializeField] private CellEffectEventChannel _cellEffectChannel;
    [SerializeField] private Material _threatMaterial;
    [SerializeField] private string _threatKey;

    [Header("Optional Part Shake (visual feedback while telegraphing)")]
    // FIXME Later, dobbiamo predisporre un SO che indichi il comportamento pre - durante -post telegrap
    [SerializeField] private bool _shakeRequiredPart;

    public override float ComputeScore(AIContext context, StepState previousState, out TargetingData targeting)
    {
        targeting = TargetingData.Empty;

        var targets = _selectionStrategy != null ? _selectionStrategy.SelectTargets(context, _areaCount) : new List<ITargettable>();
        if (targets.Count == 0) return float.NegativeInfinity;

        var shape = ShapeFactory.GetShape(_shapeType);
        var zones = new List<(Vector3 Center, List<Vector3Int> Cells)>(targets.Count);
        var allCells = new HashSet<Vector3Int>();

        foreach (var t in targets)
        {
            var gridElement = t as GridElement;
            Vector3Int cell = gridElement?.gridPosition ?? Vector3Int.FloorToInt(t.Transform.position);
            Tilemap tilemap = gridElement?.activeTilemap;

            var rawCells = shape.GetCells(cell, _radius, cell);
            var cells = new List<Vector3Int>(rawCells.Count);
            foreach (var v in rawCells)
            {
                Vector3Int c = Vector3Int.FloorToInt(v);
                if (tilemap != null && !tilemap.HasTile(c)) continue;
                cells.Add(c);
            }

            zones.Add((t.Transform.position, cells));
            foreach (var c in cells) allCells.Add(c);
        }

        if (previousState != null)
        {
            previousState.Zones = zones;
            previousState.AllCells = new List<Vector3Int>(allCells);
            previousState.SelectedTargets = targets;
        }

        return targets.Count;
    }

    public override ICommand CreateCommand(HostileCharacter caster, StepState state)
    {
        state.Extra[ThreatCellEffectChannelKey] = _cellEffectChannel;
        state.Extra[ThreatKeyKey] = _threatKey;

        if (!_shakeRequiredPart)
            return new TelegraphCommand(state.AllCells, _cellEffectChannel, _threatMaterial, _threatKey);

        Transform partTransform = state.Extra.TryGetValue(RequiredPartTransformKey, out var obj) ? obj as Transform : null;
        return new PartShakeTelegraphCommand(state.AllCells, _cellEffectChannel, _threatMaterial, _threatKey,
            caster.Transform, partTransform, state);
    }

    public override void OnRolledBack(HostileCharacter caster, StepState state)
    {
        if (_shakeRequiredPart)
        {
            // Percorso di interruzione (parte rotta / caster morto / precondizioni fallite): lo strike non
            // arriverà mai a chiudere il telegraph, quindi tocca a noi restituire il loop al caster.
            EndPartShake(state, caster != null ? caster.LifecycleAnimator : null);

            if (caster != null && state.Extra.TryGetValue(CasterOriginalScaleKey, out var csObj) && csObj is Vector3 cs && cs != Vector3.zero)
                Tween.Scale(caster.Transform, cs, 0.3f, Ease.InOutQuad);
            if (state.Extra.TryGetValue(RequiredPartTransformKey, out var ptObj) && ptObj is Transform pt && pt != null
                && state.Extra.TryGetValue(PartOriginalScaleKey, out var psObj) && psObj is Vector3 ps && ps != Vector3.zero)
                Tween.Scale(pt, ps, 0.3f, Ease.InOutQuad);
        }
        _cellEffectChannel?.RaiseEvent(new CellEffectPayload { Key = _threatKey, Cells = null });
    }
}
