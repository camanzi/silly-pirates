using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SuperSlimyBall Ability", menuName = "Abilities/Enemy/Super Slimy Ball Ability")]
public class SuperSlimyBallAbility : EnemyAbilityBase
{
    [Header("SuperSlimyBall ability configs")]
    [SerializeField] private GameObject _projectile;
    [SerializeField] private int _baseDamage = 25;
    [SerializeField] private DamageType _damageType = DamageType.Physical;
    [SerializeField] private SlimyCellDataSO _slimyCellData;
    [SerializeField] private int _slimeRadius = 1;

    /// <summary>
    /// Picks the player character whose cell-plus-neighbours footprint covers the most living
    /// player characters. Ties are broken by distance (closer wins).
    /// Returns NegativeInfinity when no living player target exists.
    /// </summary>
    protected override float ComputeScore(AIContext context, out TargetingData targeting)
    {
        // Collect all living player candidates (non-HostileCharacter, alive, targettable).
        var candidates = new List<(ITargettable target, Vector3Int cell, float dist)>();

        foreach (var state in context.TurnOrder.TurnQueue)
        {
            if (state.Agent is HostileCharacter) continue;
            if (state.Agent is not IHealthOwner ho || !ho.Health.IsAlive) continue;
            if (state.Agent is not ITargettable t) continue;

            Vector3Int cell = (t as GridElement)?.gridPosition ?? default;
            float dist = Vector3.Distance(context.Caster.Transform.position, t.Transform.position);
            candidates.Add((t, cell, dist));
        }

        if (candidates.Count == 0)
        {
            targeting = TargetingData.Empty;
            return float.NegativeInfinity;
        }

        // Build a fast lookup of all living player cell positions.
        var livingCells = new HashSet<Vector3Int>();
        foreach (var (_, cell, _) in candidates)
            livingCells.Add(cell);

        // Score each candidate by how many living players fall within its radius-1 footprint
        // (centre cell + 6 hex neighbours). Ties broken by distance (ascending).
        ITargettable bestTarget = null;
        Vector3Int   bestCell   = default;
        int          bestCount  = -1;
        float        bestDist   = float.MaxValue;

        foreach (var (target, cell, dist) in candidates)
        {
            int count = CountFootprintOverlap(cell, livingCells, _slimeRadius);

            bool isBetter = count > bestCount ||
                            (count == bestCount && dist < bestDist);

            if (isBetter)
            {
                bestTarget = target;
                bestCell   = cell;
                bestCount  = count;
                bestDist   = dist;
            }
        }

        targeting = new TargetingData(
            bestTarget.Transform.position,
            bestCell,
            true,
            bestTarget
        );

        // Normalise score: at minimum 1 character hit (the target itself), max 7.
        return Mathf.Clamp01(bestCount / 7f);
    }

    private static int CountFootprintOverlap(Vector3Int centre, HashSet<Vector3Int> livingCells, int radius)
    {
        IAreaShape circle = ShapeFactory.GetShape(ShapeType.Circle);
        int count = 0;
        foreach (Vector3 cell in circle.GetCells(centre, radius, centre))
            if (livingCells.Contains(Vector3Int.FloorToInt(cell))) count++;
        return count;
    }

    public override bool CanExecute(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        return targetingData.HasValue &&
               targetingData.Value.selectedTarget is IHealthOwner ho &&
               ho.Health.IsAlive;
    }

    public override ICommand CreateCommand(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        return new SuperSlimyBallCommand(
            caster,
            targetingData.Value.selectedTarget,
            _projectile,
            trajectoryConfigData,
            _baseDamage,
            _damageType,
            _slimyCellData,
            targetingData.Value.cellPosition,
            _slimeRadius,
            (caster as HostileCharacter)?.CritStats
        );
    }
}
