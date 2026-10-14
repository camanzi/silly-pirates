using NUnit.Framework;

namespace SillyPirates.Tests.EditMode.Grid.Footprint
{
    /// <summary>
    /// The GridFootprint value type: the HasDirection rule and the copy-producing helpers (WithDirection,
    /// Rotated, WithCount, WithShape). Expectations come from the doc comments: HexDirection is listed
    /// counter-clockwise from East with 6 values, positive rotation steps turn counter-clockwise, and the legal
    /// counts are Single 1, Line 1..9, Cluster 1..7.
    /// </summary>
    public class GridFootprintTests
    {
        // ---------------------------------------------------------------- HasDirection

        [TestCase(1)]
        [TestCase(5)]
        [TestCase(0)]
        public void HasDirection_SingleShape_ReturnsFalse(int count)
        {
            GridFootprint footprint = new GridFootprint(FootprintShape.Single, count, HexDirection.NorthWest);

            Assert.That(footprint.HasDirection, Is.False);
        }

        [Test]
        public void HasDirection_StaticSingle_ReturnsFalse()
        {
            Assert.That(GridFootprint.Single.HasDirection, Is.False);
        }

        /// <summary>A count of 0 or less clamps to 1, so these are all one-cell footprints.</summary>
        [TestCase(FootprintShape.Line, 1)]
        [TestCase(FootprintShape.Line, 0)]
        [TestCase(FootprintShape.Line, -4)]
        [TestCase(FootprintShape.Cluster, 1)]
        [TestCase(FootprintShape.Cluster, 0)]
        public void HasDirection_OneCellLineOrCluster_ReturnsFalse(FootprintShape shape, int count)
        {
            GridFootprint footprint = new GridFootprint(shape, count, HexDirection.SouthWest);

            Assert.That(footprint.HasDirection, Is.False);
        }

        [TestCase(FootprintShape.Line, 2)]
        [TestCase(FootprintShape.Line, 9)]
        [TestCase(FootprintShape.Line, 50)]
        [TestCase(FootprintShape.Cluster, 2)]
        [TestCase(FootprintShape.Cluster, 7)]
        public void HasDirection_MultiCellLineOrCluster_ReturnsTrue(FootprintShape shape, int count)
        {
            GridFootprint footprint = new GridFootprint(shape, count, HexDirection.East);

            Assert.That(footprint.HasDirection, Is.True);
        }

        // ---------------------------------------------------------------- Single / Clamped

        [Test]
        public void Single_Always_IsOneCellSingleShape()
        {
            GridFootprint footprint = GridFootprint.Single;

            Assert.That(footprint.Shape, Is.EqualTo(FootprintShape.Single));
            Assert.That(footprint.Count, Is.EqualTo(1));
        }

        [Test]
        public void Clamped_OutOfRangeCount_KeepsShapeAndDirectionAndClampsCount()
        {
            GridFootprint footprint = new GridFootprint(FootprintShape.Line, 50, HexDirection.West);

            GridFootprint clamped = footprint.Clamped();

            Assert.That(clamped, Is.EqualTo(new GridFootprint(FootprintShape.Line, 9, HexDirection.West)));
        }

        // ---------------------------------------------------------------- WithDirection

        [TestCase(0, HexDirection.East)]
        [TestCase(4, HexDirection.SouthWest)]
        [TestCase(6, HexDirection.East)]
        [TestCase(7, HexDirection.NorthEast)]
        [TestCase(-1, HexDirection.SouthEast)]
        [TestCase(-6, HexDirection.East)]
        [TestCase(-8, HexDirection.SouthWest)]
        public void WithDirection_AnyValue_WrapsIntoTheSixDirections(int raw, HexDirection expected)
        {
            GridFootprint footprint = new GridFootprint(FootprintShape.Line, 3, HexDirection.NorthWest);

            GridFootprint result = footprint.WithDirection((HexDirection)raw);

            Assert.That(result.Direction, Is.EqualTo(expected));
        }

        [Test]
        public void WithDirection_Always_KeepsShapeAndCount()
        {
            GridFootprint footprint = new GridFootprint(FootprintShape.Cluster, 5, HexDirection.East);

            GridFootprint result = footprint.WithDirection(HexDirection.West);

            Assert.That(result, Is.EqualTo(new GridFootprint(FootprintShape.Cluster, 5, HexDirection.West)));
        }

        // ---------------------------------------------------------------- Rotated

        [TestCase(HexDirection.East, 1, HexDirection.NorthEast)]
        [TestCase(HexDirection.NorthEast, 1, HexDirection.NorthWest)]
        [TestCase(HexDirection.SouthEast, 1, HexDirection.East)]
        [TestCase(HexDirection.East, -1, HexDirection.SouthEast)]
        [TestCase(HexDirection.West, -1, HexDirection.NorthWest)]
        [TestCase(HexDirection.East, 0, HexDirection.East)]
        [TestCase(HexDirection.NorthWest, 6, HexDirection.NorthWest)]
        [TestCase(HexDirection.East, 7, HexDirection.NorthEast)]
        [TestCase(HexDirection.East, -7, HexDirection.SouthEast)]
        [TestCase(HexDirection.West, -6, HexDirection.West)]
        [TestCase(HexDirection.NorthEast, -13, HexDirection.East)]
        [TestCase(HexDirection.SouthWest, 15, HexDirection.NorthEast)]
        public void Rotated_Steps_TurnsCounterClockwiseForPositiveAndWraps(
            HexDirection start, int steps, HexDirection expected)
        {
            GridFootprint footprint = new GridFootprint(FootprintShape.Line, 4, start);

            GridFootprint rotated = footprint.Rotated(steps);

            Assert.That(rotated.Direction, Is.EqualTo(expected));
        }

        [Test]
        public void Rotated_Always_KeepsShapeAndCount()
        {
            GridFootprint footprint = new GridFootprint(FootprintShape.Cluster, 4, HexDirection.East);

            GridFootprint rotated = footprint.Rotated(2);

            Assert.That(rotated, Is.EqualTo(new GridFootprint(FootprintShape.Cluster, 4, HexDirection.NorthWest)));
        }

        [Test]
        public void Rotated_PlusKThenMinusK_ReturnsTheOriginalDirection()
        {
            GridFootprint footprint = new GridFootprint(FootprintShape.Line, 3, HexDirection.SouthWest);

            for (int k = -8; k <= 8; k++)
                Assert.That(footprint.Rotated(k).Rotated(-k).Direction, Is.EqualTo(HexDirection.SouthWest), $"k={k}");
        }

        // ---------------------------------------------------------------- WithCount

        [TestCase(FootprintShape.Line, 5, 5)]
        [TestCase(FootprintShape.Line, 50, 9)]
        [TestCase(FootprintShape.Line, 0, 1)]
        [TestCase(FootprintShape.Line, -2, 1)]
        [TestCase(FootprintShape.Cluster, 7, 7)]
        [TestCase(FootprintShape.Cluster, 8, 7)]
        [TestCase(FootprintShape.Cluster, 0, 1)]
        [TestCase(FootprintShape.Single, 5, 1)]
        public void WithCount_AnyCount_ClampsToTheShapeRange(FootprintShape shape, int count, int expected)
        {
            GridFootprint footprint = new GridFootprint(shape, 2, HexDirection.NorthEast);

            GridFootprint result = footprint.WithCount(count);

            Assert.That(result, Is.EqualTo(new GridFootprint(shape, expected, HexDirection.NorthEast)));
        }

        // ---------------------------------------------------------------- WithShape

        [TestCase(FootprintShape.Line, 5, FootprintShape.Cluster, 5)]
        [TestCase(FootprintShape.Cluster, 7, FootprintShape.Line, 7)]
        [TestCase(FootprintShape.Line, 9, FootprintShape.Cluster, 7)]
        [TestCase(FootprintShape.Line, 8, FootprintShape.Cluster, 7)]
        [TestCase(FootprintShape.Line, 5, FootprintShape.Single, 1)]
        [TestCase(FootprintShape.Single, 1, FootprintShape.Line, 1)]
        public void WithShape_NewShape_KeepsLegalCountOtherwiseClamps(
            FootprintShape from, int count, FootprintShape to, int expectedCount)
        {
            GridFootprint footprint = new GridFootprint(from, count, HexDirection.West);

            GridFootprint result = footprint.WithShape(to);

            Assert.That(result, Is.EqualTo(new GridFootprint(to, expectedCount, HexDirection.West)));
        }

        /// <summary>WithShape reads the effective (clamped) count, not the raw serialized one.</summary>
        [Test]
        public void WithShape_RawCountAboveOldShapeMax_UsesTheClampedCount()
        {
            GridFootprint footprint = new GridFootprint(FootprintShape.Cluster, 9, HexDirection.East);

            GridFootprint result = footprint.WithShape(FootprintShape.Line);

            Assert.That(result.Count, Is.EqualTo(7));
        }

        // ---------------------------------------------------------------- immutability

        [Test]
        public void CopyHelpers_CalledOnAFootprint_LeaveTheOriginalUnchanged()
        {
            GridFootprint original = new GridFootprint(FootprintShape.Line, 4, HexDirection.NorthEast);
            GridFootprint snapshot = new GridFootprint(FootprintShape.Line, 4, HexDirection.NorthEast);

            original.WithDirection(HexDirection.West);
            original.Rotated(3);
            original.WithCount(9);
            original.WithShape(FootprintShape.Cluster);
            original.Clamped();

            Assert.That(original, Is.EqualTo(snapshot));
        }
    }
}
