using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "Move Ability", menuName = "Abilities/Character/Actives/Move Ability")]
public class MoveAbility : AbilityBase
{
    [Header("Cell Cost")]
    [SerializeField] private CellCostRegistrySO _cellCostRegistry;

    private class MoveCache
    {
        public List<Vector3> ReachableArea;
        public HashSet<Vector3Int> ReachableSet;
        public HashSet<Vector3Int> WalkableSet;
        public List<IMovementExtension> Extensions;
        public IMovementExtension ActiveExtension;
        public object ExtensionCache;
        public Func<Vector3Int, int> CostGetter;
    }


    public override bool IsPhaseCommand(ICommand command)
    {
        if (command is not SelectOceanCurrentEntryCommand) return false;
        return true;
    }

    public override AbilityPreviewData GetPreviewData(IInteractableElement caster, TargetingData targetingData, ref object cache)
    {
        if (caster is not GridElement gridElement || caster is not IMovable movable)
            return AbilityPreviewData.Empty;

        EnsureCache(caster, gridElement, movable, ref cache);
        var moveCache = (MoveCache)cache;

        if (moveCache.ActiveExtension != null)
            return moveCache.ActiveExtension.GetPreview(caster, targetingData, ref moveCache.ExtensionCache);

        return GetPhase1Preview(gridElement, movable, moveCache, targetingData);
    }

    public override bool CanExecute(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        if (caster is not GridElement gridElement || caster is not IMovable movable || !targetingData.HasValue)
            return false;

        EnsureCache(caster, gridElement, movable, ref cache);
        var moveCache = (MoveCache)cache;

        Vector3Int td = Vector3Int.FloorToInt(targetingData.Value.cellPosition);

        if (moveCache.ActiveExtension != null)
            return moveCache.ActiveExtension.CanExecuteOn(caster, td, ref moveCache.ExtensionCache);

        if (moveCache.ReachableSet.Contains(td) &&
            (!_gridStateData.IsOccupied(td) || _gridStateData.GetEntityAt(td).Contains(gridElement)))
            return true;

        return moveCache.Extensions.Any(ext => ext.ClaimsTarget(td) &&
               ext.GetExtensionCells(moveCache.ReachableSet, gridElement.activeTilemap).Contains(td));
    }

    public override ICommand CreateCommand(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        if (caster is not GridElement gridElement || caster is not IMovable movable) return null;

        EnsureCache(caster, gridElement, movable, ref cache);
        var moveCache = (MoveCache)cache;

        Vector3Int td = Vector3Int.FloorToInt(targetingData.Value.cellPosition);

        if (moveCache.ActiveExtension != null)
            return moveCache.ActiveExtension.CreateCommandFor(caster, td, ref moveCache.ExtensionCache);

        foreach (var ext in moveCache.Extensions)
        {
            if (ext.ClaimsTarget(td))
            {
                moveCache.ActiveExtension = ext;
                moveCache.ExtensionCache = null;
                return ext.CreateCommandFor(caster, td, ref moveCache.ExtensionCache);
            }
        }

        AbilityPreviewData preview = GetPreviewData(caster, targetingData.Value, ref cache);
        return new MoveCommand((GridCharacter)caster, preview.AffectedCells, moveCache.CostGetter);
    }


    private AbilityPreviewData GetPhase1Preview(
        GridElement gridElement, IMovable movable, MoveCache moveCache, TargetingData targetingData)
    {
        Tilemap tilemap = gridElement.activeTilemap;

        var interactionArea = new List<Vector3>(moveCache.ReachableArea);
        foreach (var ext in moveCache.Extensions)
        {
            foreach (var cell in ext.GetExtensionCells(moveCache.ReachableSet, tilemap))
                interactionArea.Add((Vector3)cell);
        }

        Vector3Int hoveredCell = Vector3Int.FloorToInt(targetingData.cellPosition);

        var claimingExt = moveCache.Extensions.FirstOrDefault(ext => ext.ClaimsTarget(hoveredCell));
        if (claimingExt != null)
        {
            Vector3Int borderCell = claimingExt.GetBorderCell(hoveredCell, tilemap);
            List<Vector3> pathToBorder = PathFindingUtils.FindPath(
                gridElement.gridPosition, borderCell,
                moveCache.WalkableSet, static (pos, set) => set.Contains(pos));
            pathToBorder.Add(targetingData.cellPosition);
            return new AbilityPreviewData(affectedCells: pathToBorder, interactionArea: interactionArea);
        }

        List<Vector3> path = moveCache.CostGetter != null
            ? PathFindingUtils.FindPath(gridElement.gridPosition, hoveredCell, moveCache.WalkableSet, static (pos, set) => set.Contains(pos), moveCache.CostGetter)
            : PathFindingUtils.FindPath(gridElement.gridPosition, hoveredCell, moveCache.WalkableSet, static (pos, set) => set.Contains(pos));

        return new AbilityPreviewData(affectedCells: path, interactionArea: interactionArea);
    }

    private void EnsureCache(IInteractableElement caster, GridElement gridElement, IMovable movable, ref object cache)
    {
        if (cache is MoveCache) return;

        Tilemap tilemap = gridElement.activeTilemap;

        Func<Vector3Int, int> costGetter = _cellCostRegistry != null
            ? _cellCostRegistry.GetMovementCost
            : null;

        var walkabilityCheck = (Func<Vector3Int, Tilemap, bool>)((pos, tm) =>
        {
            TerrainTile tile = tm.GetTile<TerrainTile>(pos);
            if (tile == null || !tile.isWalkable) return false;
            if (!_gridStateData.IsOccupied(pos)) return true;
            var entities = _gridStateData.GetEntityAt(pos);
            return entities != null && entities.All(e => e is IPassableOccupant);
        });

        var reachable = costGetter != null
            ? PathFindingUtils.FindReachableArea(gridElement.gridPosition, movable.RemainingMovementPoints, tilemap, walkabilityCheck, costGetter)
            : PathFindingUtils.FindReachableArea(gridElement.gridPosition, movable.RemainingMovementPoints, tilemap, walkabilityCheck);

        var reachableSet = new HashSet<Vector3Int>();
        foreach (Vector3 pos in reachable)
        {
            var cell = Vector3Int.FloorToInt(pos);
            if (!_gridStateData.IsOccupied(cell) ||
                (_gridStateData.GetEntityAt(cell) is { } ents && ents.All(e => e == gridElement)))
                reachableSet.Add(cell);
        }

        var reachableArea = reachable
            .Where(pos => reachableSet.Contains(Vector3Int.FloorToInt(pos)))
            .ToList();

        var walkableSet = new HashSet<Vector3Int>();
        foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin)
        {
            TerrainTile tile = tilemap.GetTile<TerrainTile>(pos);
            if (tile == null || !tile.isWalkable) continue;
            if (_gridStateData.IsOccupied(pos))
            {
                var ents = _gridStateData.GetEntityAt(pos);
                if (ents == null || !ents.All(e => e is IPassableOccupant)) continue;
            }
            walkableSet.Add(pos);
        }

        var extensions = new List<IMovementExtension>();
        if (caster is MonoBehaviour mono)
        {
            var controller = mono.GetComponent<PassiveAbilityController>();
            if (controller != null)
                extensions.AddRange(controller.GetModifiers<IMovementExtension>());
        }

        cache = new MoveCache
        {
            ReachableArea = reachableArea,
            ReachableSet = reachableSet,
            WalkableSet = walkableSet,
            Extensions = extensions,
            ActiveExtension = null,
            ExtensionCache = null,
            CostGetter = costGetter
        };
    }
}
