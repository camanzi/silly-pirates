using System.Collections.Generic;
using NUnit.Framework;

namespace SillyPirates.Tests.EditMode.Utils
{
    /// <summary>
    /// The stat arithmetic every character shares: how modifiers fold into an effective value, and how agility
    /// turns into the Action Value that decides turn order.
    ///
    /// Expected values are derived from the formulas in StatUtils — (base + flat) * (1 + pct/100) clamped to 1,
    /// and AVScale / Mathf.Max(1, agility) — never from a run.
    /// </summary>
    public class StatUtilsTests
    {
        private const float Tolerance = 0.001f;

        private static List<IAgilityModifier> Agility(params IAgilityModifier[] modifiers) =>
            new List<IAgilityModifier>(modifiers);

        private static List<IEvasionModifier> Evasion(params IEvasionModifier[] modifiers) =>
            new List<IEvasionModifier>(modifiers);

        // ---------------------------------------------------------------- EvaluateAgility

        [Test]
        public void EvaluateAgility_NoModifiers_ReturnsBaseAgility()
        {
            int result = StatUtils.EvaluateAgility(100, Agility());

            Assert.That(result, Is.EqualTo(100));
        }

        [Test]
        public void EvaluateAgility_NullModifierList_ReturnsBaseAgility()
        {
            int result = StatUtils.EvaluateAgility(100, null);

            Assert.That(result, Is.EqualTo(100));
        }

        [TestCase(20, 120)]
        [TestCase(-30, 70)]
        public void EvaluateAgility_FlatModifierOnly_AddsItToTheBase(int flat, int expected)
        {
            int result = StatUtils.EvaluateAgility(100, Agility(new FakeAgilityModifier(flat: flat)));

            Assert.That(result, Is.EqualTo(expected));
        }

        [TestCase(50f, 150)]
        [TestCase(-50f, 50)]
        public void EvaluateAgility_PercentageModifierOnly_ScalesTheBase(float percentage, int expected)
        {
            int result = StatUtils.EvaluateAgility(100, Agility(new FakeAgilityModifier(percentage: percentage)));

            Assert.That(result, Is.EqualTo(expected));
        }

        /// <summary>
        /// Order is part of the contract: the flat bonus lands first and the percentage scales the result.
        /// (90 + 10) * 1.5 = 150. The other order, 90 * 1.5 + 10, would give 145.
        /// </summary>
        [Test]
        public void EvaluateAgility_FlatAndPercentage_AppliesFlatBeforeScaling()
        {
            int result = StatUtils.EvaluateAgility(90, Agility(new FakeAgilityModifier(flat: 10, percentage: 50f)));

            Assert.That(result, Is.EqualTo(150));
        }

        /// <summary>
        /// Two modifiers: flats add up (+10 +5 = +15) and percentages add up (+10 +20 = +30) before a single
        /// scaling pass — (100 + 15) * 1.3 = 149.5, not two successive multiplications (100+15)*1.1*1.2 = 151.8.
        /// </summary>
        [Test]
        public void EvaluateAgility_MultipleModifiers_SumsFlatsAndPercentagesBeforeScalingOnce()
        {
            int result = StatUtils.EvaluateAgility(100, Agility(
                new FakeAgilityModifier(flat: 10, percentage: 10f),
                new FakeAgilityModifier(flat: 5, percentage: 20f)));

            Assert.That(result, Is.EqualTo(150)); // 149.5 rounds to even -> 150
        }

        /// <summary>
        /// A slow and a speed boost on the same character cancel out by summation, not by application order:
        /// flats -20 +10 = -10, percentages -20 +40 = +20, so (100 - 10) * 1.2 = 108.
        /// </summary>
        [Test]
        public void EvaluateAgility_OpposingModifiers_SumInsteadOfOverridingEachOther()
        {
            var slow = new FakeAgilityModifier(flat: -20, percentage: -20f);
            var boost = new FakeAgilityModifier(flat: 10, percentage: 40f);

            Assert.That(StatUtils.EvaluateAgility(100, Agility(slow, boost)), Is.EqualTo(108));
            Assert.That(StatUtils.EvaluateAgility(100, Agility(boost, slow)), Is.EqualTo(108),
                "Summation must not depend on the order the modifiers come back from the controller.");
        }

        /// <summary>
        /// Mathf.RoundToInt is Math.Round, i.e. banker's rounding: an exact .5 goes to the nearest EVEN
        /// integer, so 1.5 becomes 2 but 2.5 also becomes 2. Both values are exactly representable as floats,
        /// so this pins the real rule rather than the intuitive "always round up".
        /// </summary>
        [TestCase(1, 50f, 2)]   // 1 * 1.5 = 1.5 -> 2
        [TestCase(2, 25f, 2)]   // 2 * 1.25 = 2.5 -> 2, not 3
        public void EvaluateAgility_ExactHalfValue_RoundsToNearestEven(int baseAgility, float percentage, int expected)
        {
            int result = StatUtils.EvaluateAgility(baseAgility, Agility(new FakeAgilityModifier(percentage: percentage)));

            Assert.That(result, Is.EqualTo(expected));
        }

        /// <summary>
        /// The floor exists so BaseAV can never divide by zero, whichever way the value was driven down.
        /// </summary>
        [Test]
        public void EvaluateAgility_PenaltyBelowOne_ClampsToOne()
        {
            Assert.That(StatUtils.EvaluateAgility(10, Agility(new FakeAgilityModifier(percentage: -100f))), Is.EqualTo(1));
            Assert.That(StatUtils.EvaluateAgility(10, Agility(new FakeAgilityModifier(flat: -100))), Is.EqualTo(1));
        }

        // ---------------------------------------------------------------- EvaluateEvasion

        [Test]
        public void EvaluateEvasion_NoModifiers_ReturnsBaseEvasion()
        {
            Assert.That(StatUtils.EvaluateEvasion(50, Evasion()), Is.EqualTo(50));
        }

        [Test]
        public void EvaluateEvasion_NullModifierList_ReturnsBaseEvasion()
        {
            Assert.That(StatUtils.EvaluateEvasion(50, null), Is.EqualTo(50));
        }

        [Test]
        public void EvaluateEvasion_MultipleModifiers_AddsEveryBonus()
        {
            int result = StatUtils.EvaluateEvasion(50, Evasion(new FakeEvasionModifier(10), new FakeEvasionModifier(15)));

            Assert.That(result, Is.EqualTo(75));
        }

        /// <summary>Unlike agility there is no floor here: evasion is allowed to go negative.</summary>
        [Test]
        public void EvaluateEvasion_NegativeBonusBelowZero_IsNotClamped()
        {
            int result = StatUtils.EvaluateEvasion(10, Evasion(new FakeEvasionModifier(-30)));

            Assert.That(result, Is.EqualTo(-20));
        }

        // ---------------------------------------------------------------- BaseAV

        [TestCase(100, 100f)]
        [TestCase(200, 50f)]
        [TestCase(50, 200f)]
        [TestCase(1, 10000f)]
        [TestCase(0, 10000f)]   // Mathf.Max(1, agility) clamp
        [TestCase(-5, 10000f)]  // same clamp, negative side
        public void BaseAV_KnownAgility_ReturnsScaleDividedByAgility(int agility, float expected)
        {
            Assert.That(StatUtils.BaseAV(agility), Is.EqualTo(expected).Within(Tolerance));
        }

        // ---------------------------------------------------------------- BaseAVDelta

        /// <summary>
        /// The sign is what decides whether the agent falls back or moves up the queue, and it is the opposite
        /// of what the agility change looks like: losing agility means needing MORE Action Value per turn.
        /// </summary>
        [Test]
        public void BaseAVDelta_AgilityDropped_ReturnsPositiveDelta()
        {
            float delta = StatUtils.BaseAVDelta(oldAgility: 100, newAgility: 50);

            Assert.That(delta, Is.EqualTo(100f).Within(Tolerance)); // 200 - 100
            Assert.That(delta, Is.Positive);
        }

        [Test]
        public void BaseAVDelta_AgilityRaised_ReturnsNegativeDelta()
        {
            float delta = StatUtils.BaseAVDelta(oldAgility: 100, newAgility: 200);

            Assert.That(delta, Is.EqualTo(-50f).Within(Tolerance)); // 50 - 100
            Assert.That(delta, Is.Negative);
        }

        [Test]
        public void BaseAVDelta_AgilityUnchanged_ReturnsZero()
        {
            Assert.That(StatUtils.BaseAVDelta(100, 100), Is.EqualTo(0f).Within(Tolerance));
        }
    }
}
