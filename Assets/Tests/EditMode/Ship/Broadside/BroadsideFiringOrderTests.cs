using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace SillyPirates.Tests.EditMode.Ship.Broadside
{
    /// <summary>
    /// Pins the contract of <see cref="BroadsideFiringOrder.BuildWaves{T}"/> as stated in its XML doc: one wave per
    /// distinct priority, ascending, stable inside a wave, no empty wave for gaps, output list cleared first and
    /// filled with fresh inner lists.
    ///
    /// Int items encode their own priority in the tens digit (31 is priority 3), so the expected waves can be
    /// read straight off the input. Inside a wave the values are deliberately NOT in ascending order, so a
    /// stable split cannot be confused with a sort by value.
    /// </summary>
    public class BroadsideFiringOrderTests
    {
        private static readonly Func<int, int> TensDigit = item => item / 10;

        [Test]
        public void BuildWaves_MixedPriorities_WavesAreInAscendingPriority()
        {
            int[] items = { 31, 12, 23 };
            var waves = new List<List<int>>();

            BroadsideFiringOrder.BuildWaves(items, TensDigit, waves);

            Assert.That(waves, Is.EqualTo(new[] { new[] { 12 }, new[] { 23 }, new[] { 31 } }));
        }

        [Test]
        public void BuildWaves_InterleavedPriorities_EachWaveKeepsInputOrder()
        {
            int[] items = { 32, 15, 31, 11, 13 };
            var waves = new List<List<int>>();

            BroadsideFiringOrder.BuildWaves(items, TensDigit, waves);

            Assert.That(waves, Is.EqualTo(new[] { new[] { 15, 11, 13 }, new[] { 32, 31 } }));
        }

        [Test]
        public void BuildWaves_SinglePriorityStrings_OneWaveInInputOrder()
        {
            var items = new List<string> { "stern", "bow", "mid" };
            var waves = new List<List<string>>();

            BroadsideFiringOrder.BuildWaves(items, _ => 2, waves);

            Assert.That(waves, Is.EqualTo(new[] { new[] { "stern", "bow", "mid" } }));
        }

        [Test]
        public void BuildWaves_GapBetweenPriorities_ProducesNoEmptyWave()
        {
            int[] items = { 30, 10, 31 };
            var waves = new List<List<int>>();

            BroadsideFiringOrder.BuildWaves(items, TensDigit, waves);

            Assert.That(waves, Is.EqualTo(new[] { new[] { 10 }, new[] { 30, 31 } }));
        }

        [Test]
        public void BuildWaves_NullItems_LeavesWavesEmpty()
        {
            var waves = new List<List<int>>();

            BroadsideFiringOrder.BuildWaves(null, TensDigit, waves);

            Assert.That(waves, Is.Empty);
        }

        [Test]
        public void BuildWaves_EmptyItems_LeavesWavesEmpty()
        {
            var waves = new List<List<int>>();

            BroadsideFiringOrder.BuildWaves(Array.Empty<int>(), TensDigit, waves);

            Assert.That(waves, Is.Empty);
        }

        [Test]
        public void BuildWaves_PreFilledWavesAndNullItems_ClearsWaves()
        {
            var waves = new List<List<int>> { new List<int> { 99 } };

            BroadsideFiringOrder.BuildWaves(null, TensDigit, waves);

            Assert.That(waves, Is.Empty);
        }

        [Test]
        public void BuildWaves_PreFilledWaves_ContentIsReplaced()
        {
            var waves = new List<List<int>> { new List<int> { 99 }, new List<int> { 98 } };
            int[] items = { 20, 10 };

            BroadsideFiringOrder.BuildWaves(items, TensDigit, waves);

            Assert.That(waves, Is.EqualTo(new[] { new[] { 10 }, new[] { 20 } }));
        }

        [Test]
        public void BuildWaves_PreFilledWaves_DoesNotReuseOrTouchOldInnerLists()
        {
            var oldInner = new List<int> { 99 };
            var waves = new List<List<int>> { oldInner };
            int[] items = { 10 };

            BroadsideFiringOrder.BuildWaves(items, TensDigit, waves);

            Assert.That(waves[0], Is.Not.SameAs(oldInner));
            Assert.That(oldInner, Is.EqualTo(new[] { 99 }));
        }

        [Test]
        public void BuildWaves_CalledTwiceOnSameList_SecondCallCreatesNewInnerLists()
        {
            int[] items = { 10, 20 };
            var waves = new List<List<int>>();
            BroadsideFiringOrder.BuildWaves(items, TensDigit, waves);
            List<int> firstWave = waves[0];
            List<int> secondWave = waves[1];

            BroadsideFiringOrder.BuildWaves(items, TensDigit, waves);

            Assert.That(waves[0], Is.Not.SameAs(firstWave));
            Assert.That(waves[1], Is.Not.SameAs(secondWave));
        }

        [Test]
        public void BuildWaves_NullWaves_ThrowsArgumentNullException()
        {
            int[] items = { 10 };

            TestDelegate act = () => BroadsideFiringOrder.BuildWaves(items, TensDigit, null);

            Assert.That(act, Throws.ArgumentNullException.With.Property("ParamName").EqualTo("waves"));
        }

        [Test]
        public void BuildWaves_NullPriorityOf_ThrowsArgumentNullException()
        {
            int[] items = { 10 };
            var waves = new List<List<int>>();

            TestDelegate act = () => BroadsideFiringOrder.BuildWaves(items, null, waves);

            Assert.That(act, Throws.ArgumentNullException.With.Property("ParamName").EqualTo("priorityOf"));
        }
    }
}
