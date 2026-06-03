using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Caliber Expansion Ability", menuName = "Abilities/Character/Actives/Caliber Expansion Ability")]
public class CaliberExpansionAbility : AbilityBase
{
    [Header("Caliber Expansion Configs")]
    [SerializeField] private int _range = 1;
    [SerializeField] private int _apCost = 1;
    [SerializeField] private CaliberExpansionPassiveSO _passiveSO;
    [SerializeField] private TurnAgentEventChannel _onAnyTurnEnded;

    public override int ActionPointCost => _apCost;

    private class CaliberExpansionCache
    {
        public List<Vector3> AllRangeCells;
        public Dictionary<Vector3Int, OffensiveEquipment> EquipmentByCell;
        public HashSet<OffensiveEquipment> OffensiveEquipments;
    }

    public override AbilityPreviewData GetPreviewData(IInteractableElement caster, TargetingData targetingData, ref object cache)
    {
        if (caster is not GridElement gridElement)
            return AbilityPreviewData.Empty;

        EnsureCache(gridElement, ref cache);
        var c = (CaliberExpansionCache)cache;

        Vector3Int hoveredCell = targetingData.cellPosition;
        var affectedCells = new List<Vector3>();
        if (c.EquipmentByCell.ContainsKey(hoveredCell))
            affectedCells.Add((Vector3)hoveredCell);

        return new AbilityPreviewData(affectedCells: affectedCells, interactionArea: c.AllRangeCells);
    }

    public override bool CanExecute(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        if (caster is not GridElement gridElement) return false;
        if (!targetingData.HasValue) return false;
        if (caster is ITurnAgent turnAgent && turnAgent.RemainingActionPoints < _apCost) return false;

        EnsureCache(gridElement, ref cache);
        var c = (CaliberExpansionCache)cache;

        OffensiveEquipment targetEquipment = ResolveTarget(targetingData.Value, c);
        if (targetEquipment == null) return false;

        return targetEquipment.GetComponent<PassiveAbilityController>() != null;
    }

    public override ICommand CreateCommand(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        if (caster is not GridElement gridElement) return null;
        if (!targetingData.HasValue) return null;

        EnsureCache(gridElement, ref cache);
        var c = (CaliberExpansionCache)cache;

        OffensiveEquipment targetEquipment = ResolveTarget(targetingData.Value, c);
        if (targetEquipment == null) return null;

        if (caster is ITurnAgent turnAgent)
            turnAgent.RemainingActionPoints -= _apCost;

        return new CaliberExpansionCommand(targetEquipment.PassiveAbilityController, caster as ITurnAgent, _passiveSO, _onAnyTurnEnded);
    }

    private OffensiveEquipment ResolveTarget(TargetingData targetingData, CaliberExpansionCache c)
    {
        if (targetingData.selectedTarget is OffensiveEquipment seTarget && c.OffensiveEquipments.Contains(seTarget))
            return seTarget;
        c.EquipmentByCell.TryGetValue(targetingData.cellPosition, out var equipment);
        return equipment;
    }

    private void EnsureCache(GridElement caster, ref object cache)
    {
        if (cache is CaliberExpansionCache) return;

        Vector3Int casterPos = caster.gridPosition;
        var allRangeCells = new List<Vector3>();
        var equipmentByCell = new Dictionary<Vector3Int, OffensiveEquipment>();
        var offensiveEquipments = new HashSet<OffensiveEquipment>();

        IAreaShape circle = ShapeFactory.GetShape(ShapeType.Circle);
        foreach (Vector3 cellV3 in circle.GetCells(casterPos, _range, casterPos))
        {
            Vector3Int cell = Vector3Int.FloorToInt(cellV3);
            if (cell == casterPos) continue;

            TerrainTile tile = caster.activeTilemap?.GetTile<TerrainTile>(cell);
            if (tile == null || !tile.isWalkable) continue;

            allRangeCells.Add(cellV3);

            HashSet<GridElement> entities = _gridStateData.GetEntityAt(cell);
            if (entities == null) continue;

            foreach (GridElement entity in entities)
            {
                if (entity is OffensiveEquipment eq && eq.EquipmentType == EquipmentType.Offensive)
                {
                    equipmentByCell[cell] = eq;
                    offensiveEquipments.Add(eq);
                }
            }
        }

        cache = new CaliberExpansionCache { AllRangeCells = allRangeCells, EquipmentByCell = equipmentByCell, OffensiveEquipments = offensiveEquipments };
    }
}
