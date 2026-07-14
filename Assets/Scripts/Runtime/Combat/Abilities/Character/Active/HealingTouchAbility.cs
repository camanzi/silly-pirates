using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "Healing Touch Ability", menuName = "Abilities/Character/Actives/Healing Touch Ability")]
public class HealingTouchAbility : AbilityBase
{
    [Header("Healing Touch Configs")]
    [SerializeField] private int _range = 1;
    [SerializeField] private int _apCost = 1;
    [SerializeField] private int _flatHealAmount = 10;
    [SerializeField] [Range(0f, 100f)] private float _healPercentage = 0f;

    public override int ActionPointCost => _apCost;

    private class HealingTouchCache
    {
        public HashSet<Vector3Int> ValidCells;
    }

    public override AbilityPreviewData GetPreviewData(IInteractableElement caster, TargetingData targetingData, ref object cache)
    {
        if (caster is not GridElement gridElement)
            return AbilityPreviewData.Empty;

        EnsureCache(gridElement, ref cache);
        var healCache = (HealingTouchCache)cache;

        var interactionArea = new List<Vector3>();
        foreach (Vector3Int cell in healCache.ValidCells)
            interactionArea.Add((Vector3)cell);

        Vector3Int hoveredCell = targetingData.cellPosition;
        var affectedCells = new List<Vector3>();
        if (healCache.ValidCells.Contains(hoveredCell))
            affectedCells.Add((Vector3)hoveredCell);

        return new AbilityPreviewData(affectedCells: affectedCells, interactionArea: interactionArea);
    }

    public override bool CanExecute(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        if (caster is not GridElement) return false;
        if (!targetingData.HasValue) return false;

        if (caster is ITurnAgent turnAgent && turnAgent.RemainingActionPoints < _apCost) return false;

        return true;
    }

    public override bool IsValidTarget(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        if (caster is not GridElement gridElement) return false;
        if (!targetingData.HasValue) return false;

        EnsureCache(gridElement, ref cache);
        var healCache = (HealingTouchCache)cache;

        Vector3Int targetCell = targetingData.Value.cellPosition;

        if (!healCache.ValidCells.Contains(targetCell)) return false;

        ITargettable selected = targetingData.Value.selectedTarget;
        if (selected is not IHealthOwner ho || ho.Health == null || !ho.Health.IsAlive) return false;

        return true;
    }

    public override ICommand CreateCommand(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        if (caster is not GridElement) return null;
        if (!targetingData.HasValue) return null;

        ITargettable target = targetingData.Value.selectedTarget;
        if (target is not IHealthOwner healthOwner) return null;

        float healAmount = _flatHealAmount + (healthOwner.Health.MaxHp * (_healPercentage / 100f));

        return new HealingTouchCommand(caster, target, healAmount, _apCost);
    }

    private void EnsureCache(GridElement caster, ref object cache)
    {
        if (cache is HealingTouchCache) return;

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

        cache = new HealingTouchCache { ValidCells = validCells };
    }
}
