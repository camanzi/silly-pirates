using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Picks the broadside targets at random among the candidates. The placeholder criterion until smarter
/// selectors exist.
/// </summary>
[CreateAssetMenu(fileName = "RandomBroadsideTargetSelector", menuName = "Ship/Broadside/Selectors/Random")]
public class RandomBroadsideTargetSelectorSO : BroadsideTargetSelectorSO
{
    [Tooltip("When true a multishot equipment may hit the same target more than once (same rule as the player's manual targeting). When false it picks distinct targets, so it may return fewer than requested")]
    [SerializeField] private bool _allowRepeats = true;

    // One generator for the whole session: reseeding per call from the clock would repeat the same picks
    // for calls made within the same tick.
    private static System.Random s_sharedRandom;

    public bool AllowRepeats => _allowRepeats;

    public override void SelectTargets(in BroadsideTargetingContext context, List<ITargettable> results)
    {
        s_sharedRandom ??= new System.Random();
        SelectTargets(context, results, s_sharedRandom);
    }

    /// <summary>Overload with an injected generator, so tests can run on a fixed seed.</summary>
    internal void SelectTargets(in BroadsideTargetingContext context, List<ITargettable> results, System.Random random)
        => SelectRandom(context.Candidates, context.TargetCount, _allowRepeats, results, random);

    /// <summary>The whole rule, free of any Unity object.</summary>
    internal static void SelectRandom(IReadOnlyList<ITargettable> candidates, int count, bool allowRepeats,
                                      List<ITargettable> results, System.Random random)
    {
        results.Clear();
        if (candidates == null || candidates.Count == 0 || count <= 0) return;

        if (allowRepeats)
        {
            for (int i = 0; i < count; i++)
                results.Add(candidates[random.Next(candidates.Count)]);
            return;
        }

        // Partial Fisher-Yates over a copy: distinct picks, uniform, no retry loop.
        var pool = new List<ITargettable>(candidates);
        int picks = Mathf.Min(count, pool.Count);
        for (int i = 0; i < picks; i++)
        {
            int j = random.Next(i, pool.Count);
            (pool[i], pool[j]) = (pool[j], pool[i]);
            results.Add(pool[i]);
        }
    }

    // Domain reload disabled: drop the generator so a new play session does not inherit it.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => s_sharedRandom = null;
}
