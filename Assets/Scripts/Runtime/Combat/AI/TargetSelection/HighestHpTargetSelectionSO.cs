using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Highest HP Target Selection", menuName = "Combat/AI/Target Selection/Highest HP")]
public class HighestHpTargetSelectionSO : TargetSelectionStrategySO
{
    public override List<ITargettable> SelectTargets(AIContext context, int count)
    {
        var candidates = new List<(ITargettable t, float hp)>();
        foreach (var entry in context.TurnOrder.TurnQueue)
        {
            if (entry.Agent is HostileCharacter) continue;
            if (entry.Agent is not IHealthOwner ho || !ho.Health.IsAlive) continue;
            if (entry.Agent is not GridCharacter) continue;
            if (entry.Agent is not ITargettable t) continue;
            candidates.Add((t, ho.Health.CurrentHp));
        }
        if (candidates.Count == 0) return new List<ITargettable>();
        candidates.Sort((a, b) => b.hp.CompareTo(a.hp));
        int n = Mathf.Min(count, candidates.Count);
        var result = new List<ITargettable>(n);
        for (int i = 0; i < n; i++) result.Add(candidates[i].t);
        return result;
    }
}
