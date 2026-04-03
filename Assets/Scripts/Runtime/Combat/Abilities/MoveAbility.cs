using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "Move Ability", menuName = "Abilities/Character/Move Ability")]
public class MoveAbility : AbilityBase {

    public override bool CanExecute(IInteractableElement caster, Vector3 target)
    {
        if (caster is not GridElement gridElement) return false;

        Vector3Int cellPosition = Vector3Int.FloorToInt(target);
        if (_gridStateData.IsOccupied(cellPosition) && !_gridStateData.GetEntityAt(cellPosition).Contains(gridElement)) {
            return false;
        }
        
        AbilityPreviewData previewData = GetPreviewData(target, caster);
        return ((GridCharacter)caster).maxMoveSpeed >= previewData.AffectedCells.Count;
    }

    public override ICommand CreateCommand(IInteractableElement caster, Vector3 target, List<IInteractableElement> targetsInArea)
    {
        return new MoveCommand((GridCharacter) caster, GetPreviewData(target, caster).AffectedCells);
    }

    public override AbilityPreviewData GetPreviewData(Vector3 target, IInteractableElement caster) {
        if (caster is not GridElement gridElement) return AbilityPreviewData.Empty;

        Tilemap floorTilemap = gridElement.activeTilemap;

        HashSet<Vector3Int> walkableCache = new HashSet<Vector3Int>();
        foreach (Vector3Int pos in floorTilemap.cellBounds.allPositionsWithin)
        {
            TerrainTile tile = floorTilemap.GetTile<TerrainTile>(pos);
            
            if (tile != null && tile.isWalkable)
            {
                walkableCache.Add(pos);
            }
        }

        List<Vector3> path = PathFindingUtils.FindPath(gridElement.gridPosition, Vector3Int.FloorToInt(target), walkableCache, static (pos, cache) => cache.Contains(pos));
        return new AbilityPreviewData(affectedCells: path);
    }
}
