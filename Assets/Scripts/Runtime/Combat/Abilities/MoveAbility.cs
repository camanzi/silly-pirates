using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "Move Ability", menuName = "Abilities/Character/Move Ability")]
public class MoveAbility : AbilityBase {

    public override bool CanExecute(GridElement caster, Vector3Int target)
    {
        if (_gridStateData.IsOccupied(target) && !_gridStateData.GetEntityAt(target).Contains(caster)) {
            return false;
        }
        
        AbilityPreviewData previewData = GetPreviewData(target, caster);
        return ((GridCharacter)caster).maxMoveSpeed >= previewData.affectedCells.Count;
    }

    public override ICommand CreateCommand(GridElement caster, Vector3Int target, List<GridElement> targetsInArea)
    {
        return new MoveCommand((GridCharacter) caster, GetPreviewData(target, caster).affectedCells);
    }

    public override AbilityPreviewData GetPreviewData(Vector3 target, GridElement caster) {
        Tilemap floorTilemap = caster.activeTilemap;

        HashSet<Vector3Int> walkableCache = new HashSet<Vector3Int>();
        foreach (Vector3Int pos in floorTilemap.cellBounds.allPositionsWithin)
        {
            TerrainTile tile = floorTilemap.GetTile<TerrainTile>(pos);
            
            if (tile != null && tile.isWalkable)
            {
                walkableCache.Add(pos);
            }
        }

        List<Vector3Int> path = PathFindingUtils.FindPath(caster.gridPosition, Vector3Int.FloorToInt(target), walkableCache, static (pos, cache) => cache.Contains(pos));
        return new AbilityPreviewData(path);
    }
}
