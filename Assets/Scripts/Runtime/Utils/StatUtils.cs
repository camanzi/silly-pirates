using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The arithmetic behind a character's modified stats, and the Action Value it produces.
///
/// It lives here because GridCharacter and HostileCharacter fold their modifiers identically but do not share
/// a base class, so the formulas used to exist as four copies that could drift apart silently — plus a fifth
/// copy of the AV division inside TurnOrderDataSO. Everything is pure: no component, no scene, no allocation.
/// </summary>
public static class StatUtils
{
    /// <summary>
    /// The divisor that turns agility into an Action Value: the lower the AV, the sooner the agent acts.
    /// </summary>
    public const float AVScale = 10_000f;

    /// <summary>
    /// (base + flat) * (1 + percentage/100), rounded, never below 1.
    ///
    /// The order matters: the flat bonus is applied first and the percentage scales the result, so a +10 flat
    /// and a +50% bonus on a base of 90 give 150, not 145. An agent is always left with at least 1 agility so
    /// <see cref="BaseAV"/> can never divide by zero.
    /// </summary>
    public static int EvaluateAgility(int baseAgility, IReadOnlyList<IAgilityModifier> modifiers)
    {
        int totalFlat = 0;
        float totalPercentage = 0f;

        if (modifiers != null)
        {
            for (int i = 0; i < modifiers.Count; i++)
            {
                totalFlat += modifiers[i].GetFlatAgilityBonus();
                totalPercentage += modifiers[i].GetPercentageAgilityBonus();
            }
        }

        float modified = (baseAgility + totalFlat) * (1f + totalPercentage / 100f);
        return Mathf.Max(1, Mathf.RoundToInt(modified));
    }

    /// <summary>
    /// base + the sum of every bonus. Unlike agility there is no percentage term and no floor: evasion is
    /// consumed by MathUtils.CalculateHitChance, which already guards its own inputs.
    /// </summary>
    public static int EvaluateEvasion(int baseEvasion, IReadOnlyList<IEvasionModifier> modifiers)
    {
        int total = 0;

        if (modifiers != null)
        {
            for (int i = 0; i < modifiers.Count; i++)
                total += modifiers[i].GetEvasionBonus();
        }

        return baseEvasion + total;
    }

    /// <summary>
    /// The Action Value a full turn costs an agent of this agility. Agility is clamped to 1 first, so a
    /// misconfigured 0 yields the slowest possible agent instead of a division by zero.
    /// </summary>
    public static float BaseAV(int agility) => AVScale / Mathf.Max(1, agility);

    /// <summary>
    /// How much an agent's Action Value should shift when its agility changes.
    ///
    /// Positive means slowed down (it now needs more AV per turn, so it falls back in the queue); negative
    /// means sped up. This is the value the turn order is nudged by — see TurnOrderDataSO.AdjustAgentAV.
    /// </summary>
    public static float BaseAVDelta(int oldAgility, int newAgility) => BaseAV(newAgility) - BaseAV(oldAgility);
}
