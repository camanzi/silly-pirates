using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "Move Ability", menuName = "Character/Abilities/Move Ability")]
public class MoveAbility : AbilityBase {
    public override bool CanExecute(GridElement caster, Vector3Int target)
    {
        return true;
    }

    public override ICommand CreateCommand(GridElement caster, Vector3Int target, List<GridElement> targetsInArea)
    {
        ((InteractableGridElement) caster).isSelected = false;
        return new MoveCommand((GridCharacter) caster, GetAffectedCells(target, caster));
    }

    public override List<Vector3Int> GetAffectedCells(Vector3Int target, GridElement caster) {
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

        return PathFindingUtils.FindPath(caster.gridPosition, target, walkableCache, static (pos, cache) => cache.Contains(pos));
    }
}
