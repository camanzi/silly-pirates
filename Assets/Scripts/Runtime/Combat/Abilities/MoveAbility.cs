using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "Move Ability", menuName = "Abilities/Character/Move Ability")]
public class MoveAbility : AbilityBase {

    private class MoveCache 
    {
        public List<Vector3> ReachableArea;
    }

    public override bool CanExecute(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        if (caster is not GridElement gridElement || !targetingData.HasValue) return false;

        Vector3 target = targetingData.Value.cellPosition;
        Vector3Int cellPosition = Vector3Int.FloorToInt(target);

        if (_gridStateData.IsOccupied(cellPosition) && !_gridStateData.GetEntityAt(cellPosition).Contains(gridElement)) {
            return false;
        }
        
        if (caster is not IMovable movableElement) return false;

        AbilityPreviewData previewData = GetPreviewData(caster, targetingData.Value, ref cache);
        return movableElement.RemainingMovementPoints >= previewData.AffectedCells.Count && previewData.AffectedCells.Count > 0;
    }

    public override ICommand CreateCommand(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        MoveCommand mc = new MoveCommand((GridCharacter) caster, GetPreviewData(caster, targetingData.Value, ref cache).AffectedCells);
        return mc;
    }

    public override AbilityPreviewData GetPreviewData(IInteractableElement caster, TargetingData targetingData, ref object cache) {
        if (caster is not GridElement gridElement || caster is not IMovable movableElement) return AbilityPreviewData.Empty;

        Tilemap floorTilemap = gridElement.activeTilemap;

        if (cache == null || cache is not MoveCache)
        {            
            int maxCost = movableElement.RemainingMovementPoints; 
            
            cache = new MoveCache 
            {
                ReachableArea = PathFindingUtils.FindReachableArea(
                    gridElement.gridPosition, 
                    maxCost, 
                    floorTilemap, 
                    (pos, tilemap) => 
                    {
                        TerrainTile tile = tilemap.GetTile<TerrainTile>(pos);
                        bool existAndWalkable = tile != null && tile.isWalkable;
                        bool isOccupied = _gridStateData.IsOccupied(pos);
                        return existAndWalkable && !isOccupied;
                    }
                ) 
            };
        }

        Vector3 target = targetingData.cellPosition;
        List<Vector3> path = new List<Vector3>();

        HashSet<Vector3Int> walkableCache = new HashSet<Vector3Int>();
        foreach (Vector3Int pos in floorTilemap.cellBounds.allPositionsWithin) {
            TerrainTile tile = floorTilemap.GetTile<TerrainTile>(pos);
            if (tile != null && tile.isWalkable)
            {
                walkableCache.Add(pos);
            }
        }

        path = PathFindingUtils.FindPath(
            gridElement.gridPosition, 
            Vector3Int.FloorToInt(target), 
            walkableCache, 
            static (pos, cache) => cache.Contains(pos)
        );

        MoveCache moveCache = (MoveCache)cache;
        return new AbilityPreviewData(affectedCells: path, interactionArea: moveCache.ReachableArea);
    }
}
