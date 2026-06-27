using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "AbilityRenderer", menuName = "Combat/Abilities/Ability Renderer")]
public class AbilityRendererSO : ScriptableObject
{
    [Header("Preview Tiles")]
    [SerializeField] private Tile _hoverFloorAllowTile;
    [SerializeField] private Tile _hoverFloorNotAllowTile;
    [SerializeField] private Tile _inRangePreviewTile;

    [Header("Event Channels")]
    [SerializeField] private HighlightGridEventChannel _highlightCellsEventChannel;
    [SerializeField] private HighlightFreeAimEventChannel _targetTransformEventChannel;

    public void DrawAbilityPreview(AbilityPreviewData data, AbilityBase ability, IInteractableElement caster, TargetingData targetingData, bool canExecute)
    {
        List<TrajectoryArc> arcs = null;
        if (data.Arcs != null)
        {
            arcs = data.Arcs;
        }
        else if (ability.ShowTrajectory)
        {
            float height = ability.TrajectoryConfigData?.Height ?? 0f;
            arcs = new List<TrajectoryArc>();
            foreach (ITargettable target in data.FreeAimTargets ?? new())
                arcs.Add(new TrajectoryArc { Start = caster.Transform.position, End = target.Transform.position, PeakHeight = height });
            if (targetingData.worldPosition.HasValue)
                arcs.Add(new TrajectoryArc { Start = caster.Transform.position, End = targetingData.worldPosition.Value, PeakHeight = height });
        }

        if (arcs != null)
        {
            ITargettable hovered = targetingData.selectedTarget;
            float? hitChance = hovered != null ? ability.GetHitChance(caster, targetingData) : null;
            _targetTransformEventChannel.RaiseEvent(new HighlightFreeAimPayload(caster, canExecute, data.FreeAimTargets, arcs)
                { HoveredTarget = hovered, HitChance = hitChance });
        }

        var layers = new List<CellOverlayLayer>();
        if (data.InteractionArea != null)
            layers.Add(new CellOverlayLayer { Key = HighlightLayerKeys.PreviewInteraction, Cells = ToVector3IntList(data.InteractionArea), Tile = _inRangePreviewTile, Target = TilemapTarget.Preview });
        layers.Add(new CellOverlayLayer { Key = HighlightLayerKeys.PreviewAffected, Cells = ToVector3IntList(data.AffectedCells), Tile = canExecute ? _hoverFloorAllowTile : _hoverFloorNotAllowTile, Target = TilemapTarget.Preview });
        _highlightCellsEventChannel.RaiseEvent(new HighlightGridPayload { Layers = layers });
    }

    public void ClearPreview()
    {
        _highlightCellsEventChannel.RaiseEvent(new HighlightGridPayload
        {
            Layers = new List<CellOverlayLayer>
            {
                new() { Key = HighlightLayerKeys.PreviewAffected,    Target = TilemapTarget.Preview },
                new() { Key = HighlightLayerKeys.PreviewInteraction, Target = TilemapTarget.Preview }
            }
        });
        _targetTransformEventChannel.RaiseEvent(HighlightFreeAimPayload.Empty);
    }

    private static List<Vector3Int> ToVector3IntList(List<Vector3> list)
    {
        if (list == null) return null;
        var result = new List<Vector3Int>(list.Count);
        foreach (var v in list) result.Add(Vector3Int.FloorToInt(v));
        return result;
    }
}
