using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Maximize Contribution Ability", menuName = "Abilities/Character/Actives/Maximize Contribution Ability")]
public class MaximizeContributionAbility : AbilityBase
{
    [Header("Maximize Contribution Configs")]
    [SerializeField] private int _range = 1;
    [SerializeField] private int _apCost = 2;

    public override int ActionPointCost => _apCost;

    private class MaximizeContributionCache
    {
        public HashSet<Vector3Int> ValidCells;
        public IAwakable LastHoveredTarget;
    }

    public override AbilityPreviewData GetPreviewData(IInteractableElement caster, TargetingData targetingData, ref object cache)
    {
        if (caster is not GridElement gridElement)
            return AbilityPreviewData.Empty;

        EnsureCache(gridElement, ref cache);
        var mcCache = (MaximizeContributionCache)cache;

        var interactionArea = new List<Vector3>();
        foreach (Vector3Int cell in mcCache.ValidCells)
            interactionArea.Add((Vector3)cell);

        Vector3Int hoveredCell = targetingData.cellPosition;

        IAwakable candidate = targetingData.selectedTarget as IAwakable;
        bool eligible = mcCache.ValidCells.Contains(hoveredCell)
            && candidate != null
            && candidate.IsAwake
            && !candidate.IsOnCooldown
            && candidate.CurrentAwakeningPoints < candidate.OvercapLimit;

        int pointsToAdd = eligible ? candidate.OvercapLimit - candidate.CurrentAwakeningPoints : 0;

        IAwakable newHovered = eligible ? candidate : null;
        if (!ReferenceEquals(newHovered, mcCache.LastHoveredTarget))
        {
            if (mcCache.LastHoveredTarget != null)
                mcCache.LastHoveredTarget.OnAwakeningHoverPreview?.Invoke(0);

            if (newHovered != null)
                newHovered.OnAwakeningHoverPreview?.Invoke(pointsToAdd);

            mcCache.LastHoveredTarget = newHovered;
        }

        var affectedCells = new List<Vector3>();
        if (eligible)
            affectedCells.Add((Vector3)hoveredCell);

        return new AbilityPreviewData(affectedCells: affectedCells, interactionArea: interactionArea);
    }

    public override bool CanExecute(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        if (caster is not GridElement gridElement) return false;
        if (!targetingData.HasValue) return false;

        if (caster is ITurnAgent turnAgent && turnAgent.RemainingActionPoints < _apCost) return false;

        EnsureCache(gridElement, ref cache);
        var mcCache = (MaximizeContributionCache)cache;

        Vector3Int targetCell = targetingData.Value.cellPosition;

        if (!mcCache.ValidCells.Contains(targetCell)) return false;

        if (targetingData.Value.selectedTarget is not IAwakable a) return false;
        if (!a.IsAwake || a.IsOnCooldown || a.CurrentAwakeningPoints >= a.OvercapLimit) return false;

        return true;
    }

    public override ICommand CreateCommand(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        if (caster is not GridElement) return null;
        if (!targetingData.HasValue) return null;

        if (targetingData.Value.selectedTarget is not ShipEquipment target) return null;

        return new MaximizeContributionCommand(caster, target, _apCost);
    }

    public override void OnTargetingExit(IInteractableElement caster, ref object cache)
    {
        if (cache is MaximizeContributionCache mcCache && mcCache.LastHoveredTarget != null)
        {
            mcCache.LastHoveredTarget.OnAwakeningHoverPreview?.Invoke(0);
            mcCache.LastHoveredTarget = null;
        }
    }

    private void EnsureCache(GridElement caster, ref object cache)
    {
        if (cache is MaximizeContributionCache) return;

        Vector3Int casterPos = caster.gridPosition;
        var validCells = new HashSet<Vector3Int>();

        IAreaShape circle = ShapeFactory.GetShape(ShapeType.Circle);
        foreach (Vector3 cellV3 in circle.GetCells(casterPos, _range, casterPos))
        {
            Vector3Int cell = Vector3Int.FloorToInt(cellV3);
            validCells.Add(cell);
        }

        cache = new MaximizeContributionCache { ValidCells = validCells };
    }
}
