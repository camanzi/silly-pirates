using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "Move Ability", menuName = "Abilities/Character/Move Ability")]
public class MoveAbility : AbilityBase {

    private IInteractableElement _lastCaster;
    private List<Vector3> _cachedReachableArea;

    public override bool CanExecute(IInteractableElement caster, TargetingData? targetingData)
    {
        if (caster is not GridElement gridElement || !targetingData.HasValue) return false;

        Vector3 target = targetingData.Value.cellPosition;
        Vector3Int cellPosition = Vector3Int.FloorToInt(target);

        if (_gridStateData.IsOccupied(cellPosition) && !_gridStateData.GetEntityAt(cellPosition).Contains(gridElement)) {
            return false;
        }
        
        if (caster is not IMovable movableElement) return false;

        AbilityPreviewData previewData = GetPreviewData(caster, targetingData.Value);
        return movableElement.RemainingMovementPoints >= previewData.AffectedCells.Count && previewData.AffectedCells.Count > 0;
    }

    public override ICommand CreateCommand(IInteractableElement caster, TargetingData? targetingData)
    {
        MoveCommand mc = new MoveCommand((GridCharacter) caster, GetPreviewData(caster, targetingData.Value).AffectedCells);
        ClearCache();
        return mc;
    }

    public override AbilityPreviewData GetPreviewData(IInteractableElement caster, TargetingData targetingData) {
        if (caster is not GridElement gridElement || caster is not IMovable movableElement) return AbilityPreviewData.Empty;

        Tilemap floorTilemap = gridElement.activeTilemap;

        if (_lastCaster != caster || _cachedReachableArea == null)
        {
            _lastCaster = caster;
            
            int maxCost = movableElement.RemainingMovementPoints; 
            
            _cachedReachableArea = PathFindingUtils.FindReachableArea(
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
            );
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

        return new AbilityPreviewData(affectedCells: path, interactionArea: _cachedReachableArea);
    }

    public void ClearCache()
    {
        _lastCaster = null;
        _cachedReachableArea = null;
    }
}
