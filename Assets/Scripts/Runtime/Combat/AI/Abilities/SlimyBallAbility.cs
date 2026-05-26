using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SlimyBall Ability", menuName = "Abilities/Enemy/Slimy Ball Ability")]
public class SlimyBallAbility : EnemyAbilityBase
{
    [SerializeField] private GameObject _projectile;
    [SerializeField] private int _baseDamage = 15;
    [SerializeField] private SlimyCellDataSO _slimyCellData;

    protected override float ComputeScore(AIContext context, out TargetingData targeting)
    {
        var candidates = new List<(ITargettable target, float dist)>();

        foreach (var state in context.TurnOrder.TurnQueue)
        {
            if (state.Agent is HostileCharacter) continue;
            if (state.Agent is not IHealthOwner ho || !ho.Health.IsAlive) continue;
            if (state.Agent is not ITargettable t) continue;

            float dist = Vector3.Distance(context.Caster.Transform.position, t.Transform.position);
            candidates.Add((t, dist));
        }

        if (candidates.Count == 0)
        {
            targeting = TargetingData.Empty;
            return float.NegativeInfinity;
        }

        candidates.Sort((a, b) => a.dist.CompareTo(b.dist));

        float totalWeight = 0f;
        var weights = new float[candidates.Count];
        for (int i = 0; i < candidates.Count; i++)
        {
            weights[i] = i < 2 ? 1.25f : 1.0f;
            totalWeight += weights[i];
        }

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        ITargettable chosen = candidates[candidates.Count - 1].target;
        for (int i = 0; i < candidates.Count; i++)
        {
            cumulative += weights[i];
            if (roll < cumulative) { chosen = candidates[i].target; break; }
        }

        targeting = new TargetingData(
            chosen.Transform.position,
            (chosen as GridElement)?.gridPosition ?? default,
            true,
            chosen
        );
        return 1f;
    }

    public override bool CanExecute(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        return targetingData.HasValue &&
               targetingData.Value.selectedTarget is IHealthOwner ho &&
               ho.Health.IsAlive;
    }

    public override ICommand CreateCommand(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        return new SlimyBallCommand(
            caster,
            targetingData.Value.selectedTarget,
            _projectile,
            trajectoryConfigData,
            _baseDamage,
            _slimyCellData,
            targetingData.Value.cellPosition
        );
    }
}
