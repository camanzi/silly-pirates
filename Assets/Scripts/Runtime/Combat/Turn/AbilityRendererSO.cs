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

    // DrawAbilityPreview gira a ogni movimento del puntatore durante la mira: e' il percorso piu'
    // frequente del combat loop, e allocare qui una collection per chiamata e' la sua voce di GC
    // dominante. I buffer sono riusabili perche' nessun listener conserva le liste oltre la chiamata
    // (ShipController.ApplyLayer ne fa una copia, FreeAimRenderer e HitChanceIndicator le leggono e basta).
    private readonly List<TrajectoryArc> _arcsBuffer = new();
    private readonly List<CellOverlayLayer> _layersBuffer = new();
    private readonly List<Vector3Int> _interactionCellsBuffer = new();
    private readonly List<Vector3Int> _affectedCellsBuffer = new();

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
            arcs = _arcsBuffer;
            arcs.Clear();

            if (data.FreeAimTargets != null)
                foreach (ITargettable target in data.FreeAimTargets)
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

        _layersBuffer.Clear();
        if (data.InteractionArea != null)
            _layersBuffer.Add(new CellOverlayLayer { Key = HighlightLayerKeys.PreviewInteraction, Cells = ToVector3IntList(data.InteractionArea, _interactionCellsBuffer), Tile = _inRangePreviewTile, Target = TilemapTarget.Preview });
        _layersBuffer.Add(new CellOverlayLayer { Key = HighlightLayerKeys.PreviewAffected, Cells = ToVector3IntList(data.AffectedCells, _affectedCellsBuffer), Tile = canExecute ? _hoverFloorAllowTile : _hoverFloorNotAllowTile, Target = TilemapTarget.Preview });
        _highlightCellsEventChannel.RaiseEvent(new HighlightGridPayload { Layers = _layersBuffer });
    }

    public void ClearPreview()
    {
        _layersBuffer.Clear();
        _layersBuffer.Add(new CellOverlayLayer { Key = HighlightLayerKeys.PreviewAffected,    Target = TilemapTarget.Preview });
        _layersBuffer.Add(new CellOverlayLayer { Key = HighlightLayerKeys.PreviewInteraction, Target = TilemapTarget.Preview });

        _highlightCellsEventChannel.RaiseEvent(new HighlightGridPayload { Layers = _layersBuffer });
        _targetTransformEventChannel.RaiseEvent(HighlightFreeAimPayload.Empty);
    }

    private static List<Vector3Int> ToVector3IntList(List<Vector3> list, List<Vector3Int> buffer)
    {
        if (list == null) return null;

        buffer.Clear();
        foreach (var v in list) buffer.Add(Vector3Int.FloorToInt(v));
        return buffer;
    }
}
