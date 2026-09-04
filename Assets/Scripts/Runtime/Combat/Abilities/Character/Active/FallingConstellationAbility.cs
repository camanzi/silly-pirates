using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "Falling Constellation Ability", menuName = "Abilities/Character/Actives/Falling Constellation Ability")]
public class FallingConstellationAbility : AbilityBase, IOffensiveAbility
{
    [Header("Falling Constellation Configs")]
    [SerializeField] private int _range = 4;
    [SerializeField] private int _apCost = 1;
    [SerializeField] private ConstellationFragment _constellationPiecePrefab;

    public override int ActionPointCost => _apCost;

    private class ConstellationFallCache
    {
        public HashSet<Vector3Int> ValidCells;
    }

    public override AbilityPreviewData GetPreviewData(IInteractableElement caster, TargetingData targetingData, ref object cache)
    {
        if (caster is not GridElement gridElement)
            return AbilityPreviewData.Empty;

        EnsureCache(gridElement, ref cache);
        var fallCache = (ConstellationFallCache)cache;

        var interactionArea = new List<Vector3>();
        foreach (Vector3Int cell in fallCache.ValidCells)
            interactionArea.Add((Vector3)cell);

        Vector3Int hoveredCell = targetingData.cellPosition;
        var affectedCells = new List<Vector3>();
        if (fallCache.ValidCells.Contains(hoveredCell))
            affectedCells.Add((Vector3)hoveredCell);

        return new AbilityPreviewData(affectedCells: affectedCells, interactionArea: interactionArea);
    }

    public override bool CanExecute(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        if (caster is not GridElement gridElement) return false;
        if (!targetingData.HasValue) return false;
        if (caster is ITurnAgent turnAgent && turnAgent.RemainingActionPoints < _apCost) return false;

        EnsureCache(gridElement, ref cache);
        var fallCache = (ConstellationFallCache)cache;

        return fallCache.ValidCells.Contains(targetingData.Value.cellPosition);
    }

    public override ICommand CreateCommand(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        if (!targetingData.HasValue) return null;
        if (caster is not GridElement gridElement) return null;

        Vector3Int targetCell = targetingData.Value.cellPosition;
        Vector3 worldPos = gridElement.activeTilemap.GetCellCenterWorld(targetCell);

        return new ConstellationFallCommand(caster, targetCell, worldPos, _constellationPiecePrefab, _apCost);
    }

    private void EnsureCache(GridElement caster, ref object cache)
    {
        if (cache is ConstellationFallCache) return;

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

        cache = new ConstellationFallCache { ValidCells = validCells };
    }

    // Il danno lo infligge il ConstellationFragment quando atterra, non l'abilita': nessun elemento qui.
    public DamageType ResolveDamageElement(IInteractableElement caster) => DamageType.None;
}
