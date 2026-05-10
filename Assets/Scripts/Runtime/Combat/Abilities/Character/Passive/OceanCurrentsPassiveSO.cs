using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

[CreateAssetMenu(fileName = "Ocean Currents Passive", menuName = "Abilities/Character/Passives/Ocean Currents")]
public class OceanCurrentsPassiveSO : PassiveAbilitySO, IMovementExtension, IPassiveUIProvider, IOceanCurrentsUIReporter
{
    private static readonly Vector3Int[] OddRowNeighbors =
    {
        new(0, 1, 0), new(1, 1, 0), new(1, 0, 0),
        new(1, -1, 0), new(0, -1, 0), new(-1, 0, 0)
    };
    private static readonly Vector3Int[] EvenRowNeighbors =
    {
        new(-1, 1, 0), new(0, 1, 0), new(1, 0, 0),
        new(0, -1, 0), new(-1, -1, 0), new(-1, 0, 0)
    };

    [Header("Dependencies")]
    [SerializeField] private GridStateDataSO _gridStateData;

    [Header("UI")]
    [SerializeField] private VisualTreeAsset _uiTemplate;

    [Header("Configs")]
    [SerializeField] private int _currentCellCount = 5;
    [SerializeField] private int _exitRange = 3;
    [SerializeField] private float _jumpPeakHeight = 3f;
    [SerializeField] private float _divePeakHeight = 0.5f;
    [SerializeField] private float _diveDuration = 0.4f;
    [SerializeField] private float _exitDuration = 0.8f;

    [Header("Event Channels")]
    [SerializeField] private TurnAgentEventChannel _turnChangedChannel;
    [SerializeField] private HighlightGridEventChannel _highlightChannel;

    public bool IsAvailable => !_usedThisTurn && _currentCells.Count > 0;
    public event Action OnStateUpdated;

    private GridCharacter _owner;
    private List<Vector3Int> _currentCells = new();
    private bool _usedThisTurn;
    private UnityAction<ITurnAgent> _onTurnChanged;
    private Action _onUsedDelegate;

    private readonly List<Vector3> _previewAffectedCells = new(1);
    private static readonly List<Vector3> _emptyInteractionArea = new();
    private readonly HashSet<Vector3> _exitCellsSet = new();
    private readonly List<Vector3> _exitCellsList = new();

    // Phase 2 cache — stored inside extCache passed by MoveAbility
    private class Phase2Cache
    {
        public Vector3Int EntryCell;
        public Vector3Int BorderCell;
        public HashSet<Vector3Int> WalkableSet;
    }

    // --- PassiveAbilitySO ---

    public override void OnEquip(PassiveAbilityController controller)
    {
        _owner = controller.GetComponent<GridCharacter>();
        _currentCells = new List<Vector3Int>();
        _usedThisTurn = false;
        _onTurnChanged = OnTurnChanged;
        _onUsedDelegate = () => { _usedThisTurn = true; OnStateUpdated?.Invoke(); };
        _turnChangedChannel.OnEventRaised += _onTurnChanged;
    }

    public override void OnUnequip(PassiveAbilityController controller)
    {
        _turnChangedChannel.OnEventRaised -= _onTurnChanged;
        _currentCells.Clear();
        _highlightChannel?.RaiseEvent(HighlightGridPayload.OceanCurrentUpdate(new List<Vector3Int>()));
    }

    private void OnTurnChanged(ITurnAgent agent)
    {
        if (agent != (ITurnAgent)_owner)
        {
            _currentCells.Clear();
            _highlightChannel?.RaiseEvent(HighlightGridPayload.OceanCurrentUpdate(new List<Vector3Int>()));
            return;
        }

        Tilemap tilemap = _owner.activeTilemap;
        if (tilemap == null) return;

        Vector3 center = tilemap.cellBounds.center;
        List<Vector3Int> candidates = OceanCurrentDistributor.FindExteriorBorderCells(tilemap);
        _currentCells = OceanCurrentDistributor.Distribute(candidates, _currentCellCount, center, UnityEngine.Random.Range(0, 100000));
        _usedThisTurn = false;
        _highlightChannel?.RaiseEvent(HighlightGridPayload.OceanCurrentUpdate(_currentCells));
        OnStateUpdated?.Invoke();
    }


    // --- IPassiveUIProvider ---

    public VisualTreeAsset GetTemplate() => _uiTemplate;

    public VisualElement CreateUIElement(VisualTreeAsset template) => new OceanCurrentsIndicator(this, template);

    // --- IMovementExtension ---

    public IEnumerable<Vector3Int> GetExtensionCells(HashSet<Vector3Int> reachableSet, Tilemap tilemap)
    {
        if (_usedThisTurn) yield break;
        foreach (var cell in _currentCells)
        {
            if (IsCurrentCellReachable(cell, reachableSet))
                yield return cell;
        }
    }

    public bool ClaimsTarget(Vector3Int cell)
        => !_usedThisTurn && _currentCells.Contains(cell);

    public AbilityPreviewData GetPreview(IInteractableElement caster, TargetingData td, ref object extCache)
    {
        if (caster is not GridElement gridElement) return AbilityPreviewData.Empty;

        if (extCache is Phase2Cache p2)
            return GetPhase2Preview(gridElement, p2, td);

        _previewAffectedCells.Clear();
        _previewAffectedCells.Add(td.cellPosition);
        return new AbilityPreviewData(
            affectedCells: _previewAffectedCells,
            interactionArea: _emptyInteractionArea
        );
    }

    public bool CanExecuteOn(IInteractableElement caster, Vector3Int td, ref object extCache)
    {
        if (caster is not GridElement gridElement) return false;

        if (extCache is Phase2Cache p2)
            return CanExecutePhase2(gridElement, p2, td);

        return _currentCells.Contains(td);
    }

    public ICommand CreateCommandFor(IInteractableElement caster, Vector3Int td, ref object extCache)
    {
        if (caster is not GridElement gridElement) return null;

        if (extCache is not Phase2Cache)
        {
            Vector3Int borderCell = FindBorderCell(gridElement.gridPosition, td, gridElement.activeTilemap);
            var walkableSet = BuildWalkableSet(gridElement.activeTilemap);
            extCache = new Phase2Cache
            {
                EntryCell = td,
                BorderCell = borderCell,
                WalkableSet = walkableSet
            };
            return new SelectOceanCurrentEntryCommand();
        }

        var p2 = (Phase2Cache)extCache;
        List<Vector3> pathToBorder = PathFindingUtils.FindPath(
            gridElement.gridPosition, p2.BorderCell,
            p2.WalkableSet, static (pos, set) => set.Contains(pos)
        );

        Vector3Int exitSource = _currentCells
            .Where(c => c != p2.EntryCell)
            .OrderBy(c => PathFindingUtils.GetDistance(c, td))
            .First();

        return new OceanCurrentJumpCommand(
            (GridCharacter)caster,
            pathToBorder,
            p2.EntryCell,
            exitSource,
            td,
            _onUsedDelegate,
            _jumpPeakHeight,
            _divePeakHeight,
            _diveDuration,
            _exitDuration
        );
    }

    public bool IsPhaseCommand(ICommand cmd) => cmd is SelectOceanCurrentEntryCommand;

    public Vector3Int GetBorderCell(Vector3Int extensionCell, Tilemap tilemap) => FindBorderCell(_owner.gridPosition, extensionCell, tilemap);

    private AbilityPreviewData GetPhase2Preview(GridElement gridElement, Phase2Cache p2, TargetingData td)
    {
        Tilemap tilemap = gridElement.activeTilemap;
        var exitCells = ComputeExitCells(gridElement, p2.EntryCell, tilemap);

        Vector3 borderWorld = tilemap.GetCellCenterWorld(p2.BorderCell);
        Vector3 entryUnder = tilemap.GetCellCenterWorld(p2.EntryCell) + Vector3.down * 2f;

        var arcs = new List<TrajectoryArc>
        {
            new() { Start = borderWorld, End = entryUnder, PeakHeight = _divePeakHeight }
        };

        Vector3Int hoveredCell = Vector3Int.FloorToInt(td.cellPosition);
        bool validExit = exitCells.Contains((Vector3)hoveredCell);

        if (validExit)
        {
            Vector3Int exitSource = _currentCells[0];
            int minDist = int.MaxValue;
            foreach (var c in _currentCells)
            {
                if (c == p2.EntryCell) continue;
                int d = PathFindingUtils.GetDistance(c, hoveredCell);
                if (d < minDist) { minDist = d; exitSource = c; }
            }
            Vector3 exitSourceUnder = tilemap.GetCellCenterWorld(exitSource) + Vector3.down * 2f;
            Vector3 exitWorld = tilemap.GetCellCenterWorld(hoveredCell);
            arcs.Add(new TrajectoryArc { Start = exitSourceUnder, End = exitWorld, PeakHeight = _jumpPeakHeight });
        }

        return new AbilityPreviewData(
            affectedCells: validExit ? new List<Vector3> { td.cellPosition } : new List<Vector3>(),
            interactionArea: exitCells,
            customArcs: arcs
        );
    }

    private bool CanExecutePhase2(GridElement gridElement, Phase2Cache p2, Vector3Int td)
    {
        Tilemap tilemap = gridElement.activeTilemap;
        TerrainTile tile = tilemap.GetTile<TerrainTile>(td);
        if (tile == null || !tile.isWalkable) return false;
        if (td == p2.EntryCell) return false;
        if (_gridStateData != null && _gridStateData.IsOccupied(td) &&
            !_gridStateData.GetEntityAt(td).Contains(gridElement)) return false;

        foreach (var src in _currentCells)
        {
            if (src == p2.EntryCell) continue;
            if (PathFindingUtils.GetNormalizedDistance(td, src) <= _exitRange) return true;
        }
        return false;
    }

    // --- Helpers ---

    private List<Vector3> ComputeExitCells(GridElement gridElement, Vector3Int entryCell, Tilemap tilemap)
    {
        _exitCellsSet.Clear();

        foreach (Vector3Int src in _currentCells)
        {
            if (src == entryCell) continue;
            foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin)
            {
                TerrainTile tile = tilemap.GetTile<TerrainTile>(pos);
                if (tile == null || !tile.isWalkable) continue;
                if (_gridStateData != null && _gridStateData.IsOccupied(pos) &&
                    !_gridStateData.GetEntityAt(pos).Contains(gridElement)) continue;
                if (PathFindingUtils.GetNormalizedDistance(pos, src) <= _exitRange)
                    _exitCellsSet.Add((Vector3)pos);
            }
        }

        _exitCellsList.Clear();
        foreach (var c in _exitCellsSet) _exitCellsList.Add(c);
        return _exitCellsList;
    }

    private static bool IsCurrentCellReachable(Vector3Int currentCell, HashSet<Vector3Int> reachableSet)
    {
        Vector3Int[] dirs = (currentCell.y & 1) != 0 ? OddRowNeighbors : EvenRowNeighbors;
        foreach (var dir in dirs)
        {
            if (reachableSet.Contains(currentCell + dir)) return true;
        }
        return false;
    }

    private static Vector3Int FindBorderCell(Vector3Int charPos, Vector3Int currentCell, Tilemap tilemap)
    {
        Vector3Int[] dirs = (currentCell.y & 1) != 0 ? OddRowNeighbors : EvenRowNeighbors;
        Vector3Int best = charPos;
        int bestDist = int.MaxValue;

        foreach (var dir in dirs)
        {
            Vector3Int neighbor = currentCell + dir;
            TerrainTile tile = tilemap.GetTile<TerrainTile>(neighbor);
            if (tile != null && tile.isWalkable)
            {
                int dist = PathFindingUtils.GetDistance(charPos, neighbor);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = neighbor;
                }
            }
        }
        return best;
    }

    private HashSet<Vector3Int> BuildWalkableSet(Tilemap tilemap)
    {
        var set = new HashSet<Vector3Int>();
        foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin)
        {
            TerrainTile tile = tilemap.GetTile<TerrainTile>(pos);
            if (tile != null && tile.isWalkable && !_gridStateData.IsOccupied(pos)) set.Add(pos);
        }
        return set;
    }
}
