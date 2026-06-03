using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "Restoring Aura Ability", menuName = "Abilities/Equipment/Restoring Aura Ability")]
public class RestoringAuraAbility : AbilityBase
{
    [Header("Restoring Aura Configs")]
    [SerializeField] private int _range = 10;
    [SerializeField] private int _cooldown = 3;
    [SerializeField] [Range(0f, 100f)] private float _reviveHpPercentage = 1f;

    private class RestoringAuraCache
    {
        public HashSet<Vector3Int> ValidCells;
    }

    public override AbilityPreviewData GetPreviewData(IInteractableElement caster, TargetingData targetingData, ref object cache)
    {
        if (caster is not GridElement gridElement)
            return AbilityPreviewData.Empty;

        EnsureCache(gridElement, ref cache);
        var auraCache = (RestoringAuraCache)cache;

        var interactionArea = new List<Vector3>();
        foreach (Vector3Int cell in auraCache.ValidCells)
            interactionArea.Add((Vector3)cell);

        Vector3Int hoveredCell = targetingData.cellPosition;
        var affectedCells = new List<Vector3>();
        if (auraCache.ValidCells.Contains(hoveredCell) && HasDeadAllyAt(hoveredCell))
            affectedCells.Add((Vector3)hoveredCell);

        return new AbilityPreviewData(affectedCells: affectedCells, interactionArea: interactionArea);
    }

    public override bool CanExecute(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        if (caster is not GridElement gridElement) return false;
        if (!targetingData.HasValue) return false;

        EnsureCache(gridElement, ref cache);
        var auraCache = (RestoringAuraCache)cache;

        Vector3Int targetCell = targetingData.Value.cellPosition;
        if (!auraCache.ValidCells.Contains(targetCell)) return false;

        ITargettable selected = targetingData.Value.selectedTarget;
        if (selected is not GridCharacter gc || gc.Health == null || gc.Health.IsAlive) return false;

        return true;
    }

    public override ICommand CreateCommand(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        if (caster is not GridElement) return null;
        if (!targetingData.HasValue) return null;

        if (targetingData.Value.selectedTarget is not GridCharacter target) return null;

        float overcapBonus = 0f;
        if (caster is IAwakable awakable)
        {
            int extraPoints = Mathf.Max(0, awakable.CurrentAwakeningPoints - awakable.MaxAwakeningPoints);
            if (extraPoints > 0)
                overcapBonus = Mathf.Pow(2, extraPoints + 1) - 2f;
        }

        float effectiveHpPercentage = (_reviveHpPercentage + overcapBonus) / 100f;
        return new RestoringAuraCommand(caster, target, effectiveHpPercentage, _cooldown);
    }

    private bool HasDeadAllyAt(Vector3Int cell)
    {
        var entities = _gridStateData?.GetEntityAt(cell);
        if (entities == null) return false;
        foreach (var entity in entities)
            if (entity is GridCharacter gc && gc.Health != null && !gc.Health.IsAlive)
                return true;
        return false;
    }

    private void EnsureCache(GridElement caster, ref object cache)
    {
        if (cache is RestoringAuraCache) return;

        Vector3Int casterPos = caster.gridPosition;
        Tilemap tilemap = caster.activeTilemap;
        var validCells = new HashSet<Vector3Int>();

        IAreaShape circle = ShapeFactory.GetShape(ShapeType.Circle);
        foreach (Vector3 cellV3 in circle.GetCells(casterPos, _range, casterPos))
        {
            Vector3Int cell = Vector3Int.FloorToInt(cellV3);

            if (tilemap != null)
            {
                TerrainTile tile = tilemap.GetTile<TerrainTile>(cell);
                if (tile == null || !tile.isWalkable) continue;
            }

            validCells.Add(cell);
        }

        cache = new RestoringAuraCache { ValidCells = validCells };
    }
}
