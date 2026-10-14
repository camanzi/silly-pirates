using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SillyPirates.Tests.EditMode.Grid.Footprint
{
    /// <summary>
    /// Footprint geometry on sets of cells. The footprints used as data are written out as literal cells
    /// (odd-r layout), so the expectations do not go through HexCoordinates.
    /// </summary>
    public class HexFootprintTests
    {
        private static Vector3Int Cell(int x, int y, int z = 0) => new Vector3Int(x, y, z);

        // A straight row of three on even row 2.
        private static readonly Vector3Int[] RowAt2_2 = { Cell(1, 2), Cell(2, 2), Cell(3, 2) };

        // A North-East diagonal of three through odd row (2,3): SouthWest is (2,2), NorthEast is (3,4).
        private static readonly Vector3Int[] DiagonalAt2_3 = { Cell(2, 3), Cell(2, 2), Cell(3, 4) };

        // (2,2) plus its West, SouthWest and SouthEast neighbours (even row).
        private static readonly Vector3Int[] WedgeAt2_2 = { Cell(2, 2), Cell(1, 2), Cell(1, 1), Cell(2, 1) };

        // Cube direction vectors, written out (not read from HexCoordinates) so the expectations stand alone.
        private static readonly Vector3Int CubeEast = new Vector3Int(1, 0, -1);
        private static readonly Vector3Int CubeNorthEast = new Vector3Int(0, 1, -1);
        private static readonly Vector3Int CubeNorthWest = new Vector3Int(-1, 1, 0);
        private static readonly Vector3Int CubeWest = new Vector3Int(-1, 0, 1);
        private static readonly Vector3Int CubeSouthWest = new Vector3Int(0, -1, 1);
        private static readonly Vector3Int CubeSouthEast = new Vector3Int(1, -1, 0);

        /// <summary>
        /// The six odd-r neighbours of an anchor in E, NE, NW, W, SW, SE order. Even rows: diagonals go to x-1
        /// and x; odd rows: to x and x+1 (odd rows are shifted right).
        /// </summary>
        private static Vector3Int[] NeighboursOf(Vector3Int anchor)
        {
            int x = anchor.x, y = anchor.y, z = anchor.z;
            int left = (y & 1) == 0 ? x - 1 : x;
            int right = left + 1;
            return new[]
            {
                new Vector3Int(x + 1, y, z),
                new Vector3Int(right, y + 1, z),
                new Vector3Int(left, y + 1, z),
                new Vector3Int(x - 1, y, z),
                new Vector3Int(left, y - 1, z),
                new Vector3Int(right, y - 1, z),
            };
        }

        private static readonly Vector3Int[] SixDirections =
        {
            CubeEast, CubeNorthEast, CubeNorthWest, CubeWest, CubeSouthWest, CubeSouthEast,
        };

        // ---------------------------------------------------------------- ToOffsets

        [TestCase(2, 2)]
        [TestCase(2, 3)]
        [TestCase(0, -1)]
        [TestCase(-3, -2)]
        public void ToOffsets_AnchorOnly_ReturnsTheZeroOffset(int x, int y)
        {
            List<Vector3Int> results = new List<Vector3Int>();

            HexFootprint.ToOffsets(Cell(x, y), new[] { Cell(x, y) }, results);

            Assert.That(results, Is.EqualTo(new[] { Vector3Int.zero }));
        }

        [TestCase(2, 2)]
        [TestCase(2, 3)]
        [TestCase(0, -1)]
        [TestCase(-3, -2)]
        public void ToOffsets_SixNeighbours_MapToTheSixCubeDirections(int x, int y)
        {
            Vector3Int anchor = Cell(x, y);
            List<Vector3Int> cells = new List<Vector3Int> { anchor };
            cells.AddRange(NeighboursOf(anchor));
            List<Vector3Int> results = new List<Vector3Int>();

            HexFootprint.ToOffsets(anchor, cells, results);

            List<Vector3Int> expected = new List<Vector3Int> { Vector3Int.zero };
            expected.AddRange(SixDirections);
            Assert.That(results, Is.EqualTo(expected));
        }

        [Test]
        public void ToOffsets_CellsWithoutTheAnchor_StillPutsTheZeroOffsetFirst()
        {
            List<Vector3Int> results = new List<Vector3Int>();

            HexFootprint.ToOffsets(Cell(2, 2), new[] { Cell(3, 2), Cell(1, 2) }, results);

            Assert.That(results, Is.EqualTo(new[] { Vector3Int.zero, CubeEast, CubeWest }));
        }

        [Test]
        public void ToOffsets_AnchorNotFirstInCells_ZeroOffsetIsFirstAndNotRepeated()
        {
            List<Vector3Int> results = new List<Vector3Int>();

            HexFootprint.ToOffsets(Cell(2, 2), new[] { Cell(3, 2), Cell(2, 2), Cell(1, 2) }, results);

            Assert.That(results, Is.EqualTo(new[] { Vector3Int.zero, CubeEast, CubeWest }));
        }

        [Test]
        public void ToOffsets_DuplicateCells_AreKeptOnce()
        {
            List<Vector3Int> results = new List<Vector3Int>();

            HexFootprint.ToOffsets(Cell(2, 2), new[] { Cell(3, 2), Cell(3, 2), Cell(2, 2), Cell(3, 2) }, results);

            Assert.That(results, Is.EqualTo(new[] { Vector3Int.zero, CubeEast }));
        }

        [Test]
        public void ToOffsets_NullCells_ReturnsTheZeroOffsetOnly()
        {
            List<Vector3Int> results = new List<Vector3Int>();

            HexFootprint.ToOffsets(Cell(2, 2), null, results);

            Assert.That(results, Is.EqualTo(new[] { Vector3Int.zero }));
        }

        [Test]
        public void ToOffsets_NonEmptyResults_ClearsThemFirst()
        {
            List<Vector3Int> results = new List<Vector3Int> { Cell(7, 7), Cell(8, 8) };

            HexFootprint.ToOffsets(Cell(2, 2), new[] { Cell(3, 2) }, results);

            Assert.That(results, Is.EqualTo(new[] { Vector3Int.zero, CubeEast }));
        }

        /// <summary>The layer (offset Z) does not leak into the cube offset: Translate re-applies the anchor's.</summary>
        [Test]
        public void ToOffsets_CellOnAnotherLayer_GivesTheSameOffsetAsOnTheAnchorLayer()
        {
            List<Vector3Int> results = new List<Vector3Int>();

            HexFootprint.ToOffsets(Cell(2, 2, 0), new[] { Cell(3, 2, 5) }, results);

            Assert.That(results, Is.EqualTo(new[] { Vector3Int.zero, CubeEast }));
        }

        // ---------------------------------------------------------------- Translate

        [Test]
        public void Translate_NullOffsets_ReturnsTheAnchorOnly()
        {
            List<Vector3Int> results = new List<Vector3Int>();

            HexFootprint.Translate(Cell(4, 1, 2), null, results);

            Assert.That(results, Is.EqualTo(new[] { Cell(4, 1, 2) }));
        }

        [Test]
        public void Translate_EmptyOffsets_ReturnsTheAnchorOnly()
        {
            List<Vector3Int> results = new List<Vector3Int>();

            HexFootprint.Translate(Cell(4, 1), new List<Vector3Int>(), results);

            Assert.That(results, Is.EqualTo(new[] { Cell(4, 1) }));
        }

        [Test]
        public void Translate_ZeroOffsetsAnywhere_AreSkippedAndTheAnchorAppearsOnce()
        {
            List<Vector3Int> results = new List<Vector3Int>();

            HexFootprint.Translate(Cell(2, 2), new[] { CubeEast, Vector3Int.zero, CubeWest, Vector3Int.zero }, results);

            Assert.That(results, Is.EqualTo(new[] { Cell(2, 2), Cell(3, 2), Cell(1, 2) }));
        }

        [Test]
        public void Translate_NonEmptyResults_ClearsThemFirst()
        {
            List<Vector3Int> results = new List<Vector3Int> { Cell(7, 7), Cell(8, 8) };

            HexFootprint.Translate(Cell(2, 2), new[] { Vector3Int.zero }, results);

            Assert.That(results, Is.EqualTo(new[] { Cell(2, 2) }));
        }

        [Test]
        public void Translate_AnchorOnALayer_EveryCellKeepsTheAnchorLayer()
        {
            List<Vector3Int> results = new List<Vector3Int>();

            HexFootprint.Translate(Cell(2, 3, 4), SixDirections, results);

            Assert.That(results.ConvertAll(c => c.z), Has.All.EqualTo(4));
        }

        /// <summary>The same cube offsets land on the correct odd-r neighbours whatever the anchor row parity.</summary>
        [TestCase(2, 2)]
        [TestCase(2, 3)]
        [TestCase(0, -1)]
        [TestCase(-3, -2)]
        public void Translate_SixCubeDirections_LandOnTheSixOddRNeighbours(int x, int y)
        {
            Vector3Int anchor = Cell(x, y);
            List<Vector3Int> results = new List<Vector3Int>();

            HexFootprint.Translate(anchor, SixDirections, results);

            List<Vector3Int> expected = new List<Vector3Int> { anchor };
            expected.AddRange(NeighboursOf(anchor));
            Assert.That(results, Is.EqualTo(expected));
        }

        /// <summary>
        /// A NE-SW diagonal moved from an even row to an odd row: in offset space the diagonal cells shift by a
        /// different x on each parity, so a naive offset-space translation would bend the line.
        /// </summary>
        [Test]
        public void Translate_DiagonalFromEvenToOddRow_KeepsTheDiagonalShape()
        {
            Vector3Int[] diagonalAt2_2 = { Cell(2, 2), Cell(2, 3), Cell(1, 1) };
            List<Vector3Int> offsets = new List<Vector3Int>();
            List<Vector3Int> results = new List<Vector3Int>();
            HexFootprint.ToOffsets(Cell(2, 2), diagonalAt2_2, offsets);

            HexFootprint.Translate(Cell(2, 3), offsets, results);

            // From odd row (2,3): NorthEast is (3,4), SouthWest is (2,2).
            Assert.That(results, Is.EqualTo(new[] { Cell(2, 3), Cell(3, 4), Cell(2, 2) }));
        }

        [Test]
        public void Translate_DiagonalFromOddToEvenRow_KeepsTheDiagonalShape()
        {
            List<Vector3Int> offsets = new List<Vector3Int>();
            List<Vector3Int> results = new List<Vector3Int>();
            HexFootprint.ToOffsets(Cell(2, 3), DiagonalAt2_3, offsets);

            HexFootprint.Translate(Cell(5, 6), offsets, results);

            // DiagonalAt2_3 is anchor, SouthWest, NorthEast. From even row (5,6): SW is (4,5), NE is (5,7).
            Assert.That(results, Is.EqualTo(new[] { Cell(5, 6), Cell(4, 5), Cell(5, 7) }));
        }

        // ---------------------------------------------------------------- ToOffsets + Translate round trip

        [TestCase(2, 2)]
        [TestCase(2, 3)]
        [TestCase(0, -1)]
        [TestCase(-3, -2)]
        public void ToOffsetsThenTranslate_SameAnchor_ReturnsTheOriginalCells(int x, int y)
        {
            Vector3Int anchor = Cell(x, y, 1);
            // Anchor first, a ring neighbour, a disconnected cell two rows up and a cell on a row of each parity.
            List<Vector3Int> cells = new List<Vector3Int>
            {
                anchor, NeighboursOf(anchor)[1], Cell(x + 3, y + 2, 1), Cell(x - 2, y - 1, 1), Cell(x, y - 3, 1),
            };
            List<Vector3Int> offsets = new List<Vector3Int>();
            List<Vector3Int> results = new List<Vector3Int>();

            HexFootprint.ToOffsets(anchor, cells, offsets);
            HexFootprint.Translate(anchor, offsets, results);

            Assert.That(results, Is.EqualTo(cells));
        }

        /// <summary>Offsets read back from a footprint placed on an even-row and on an odd-row anchor.</summary>
        [TestCase(7, 4)]
        [TestCase(7, 5)]
        [TestCase(-4, 0)]
        [TestCase(1, -7)]
        public void TranslateThenToOffsets_AnyAnchor_ReturnsTheSameOffsets(int x, int y)
        {
            Vector3Int[] offsets = { Vector3Int.zero, CubeNorthEast, CubeSouthWest * 2, new Vector3Int(2, -3, 1) };
            List<Vector3Int> cells = new List<Vector3Int>();
            List<Vector3Int> offsetsBack = new List<Vector3Int>();

            HexFootprint.Translate(Cell(x, y), offsets, cells);
            HexFootprint.ToOffsets(Cell(x, y), cells, offsetsBack);

            Assert.That(offsetsBack, Is.EqualTo(offsets));
        }

        // ---------------------------------------------------------------- DistanceTo

        [Test]
        public void DistanceTo_TargetOutside_ReturnsDistanceFromTheClosestCell()
        {
            Vector3Int[] cells = { Cell(0, 0), Cell(4, 0) };

            int distance = HexFootprint.DistanceTo(cells, Cell(5, 0));

            Assert.That(distance, Is.EqualTo(1));
        }

        [Test]
        public void DistanceTo_CellOrderReversed_ReturnsTheSameMinimum()
        {
            Vector3Int[] cells = { Cell(4, 0), Cell(0, 0) };

            int distance = HexFootprint.DistanceTo(cells, Cell(-1, 0));

            Assert.That(distance, Is.EqualTo(1));
        }

        [Test]
        public void DistanceTo_TargetInsideFootprint_ReturnsZero()
        {
            int distance = HexFootprint.DistanceTo(RowAt2_2, Cell(3, 2));

            Assert.That(distance, Is.EqualTo(0));
        }

        [Test]
        public void DistanceTo_EmptyCells_ReturnsIntMaxValue()
        {
            int distance = HexFootprint.DistanceTo(new List<Vector3Int>(), Cell(0, 0));

            Assert.That(distance, Is.EqualTo(int.MaxValue));
        }

        // ---------------------------------------------------------------- Intersects

        [Test]
        public void Intersects_OneSharedCell_ReturnsTrue()
        {
            HashSet<Vector3Int> set = new HashSet<Vector3Int> { Cell(9, 9), Cell(3, 2) };

            Assert.That(HexFootprint.Intersects(RowAt2_2, set), Is.True);
        }

        [Test]
        public void Intersects_DisjointSets_ReturnsFalse()
        {
            HashSet<Vector3Int> set = new HashSet<Vector3Int> { Cell(4, 2), Cell(2, 3) };

            Assert.That(HexFootprint.Intersects(RowAt2_2, set), Is.False);
        }

        [Test]
        public void Intersects_EmptyCells_ReturnsFalse()
        {
            HashSet<Vector3Int> set = new HashSet<Vector3Int> { Cell(0, 0) };

            Assert.That(HexFootprint.Intersects(new List<Vector3Int>(), set), Is.False);
        }

        [Test]
        public void Intersects_EmptySet_ReturnsFalse()
        {
            Vector3Int[] cells = { Cell(0, 0) };

            Assert.That(HexFootprint.Intersects(cells, new HashSet<Vector3Int>()), Is.False);
        }

        // ---------------------------------------------------------------- CollectCellsInRange

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void CollectCellsInRange_SingleCell_EqualsCircleShape(int range)
        {
            Vector3Int center = Cell(3, -2);
            HashSet<Vector3Int> results = new HashSet<Vector3Int>();

            HexFootprint.CollectCellsInRange(new[] { center }, range, results);

            List<Vector3> circle = new CircleShape().GetCells(center, range, center);
            Assert.That(results, Is.EquivalentTo(PathAssert.ToCells(circle)));
        }

        [TestCase(1)]
        [TestCase(2)]
        public void CollectCellsInRange_MultipleCells_EqualsUnionOfCircles(int range)
        {
            HashSet<Vector3Int> expected = new HashSet<Vector3Int>();
            CircleShape circle = new CircleShape();
            foreach (Vector3Int cell in DiagonalAt2_3)
                expected.UnionWith(PathAssert.ToCells(circle.GetCells(cell, range, cell)));
            HashSet<Vector3Int> results = new HashSet<Vector3Int>();

            HexFootprint.CollectCellsInRange(DiagonalAt2_3, range, results);

            Assert.That(results, Is.EquivalentTo(expected));
        }

        [Test]
        public void CollectCellsInRange_RangeZero_ReturnsTheFootprintItself()
        {
            HashSet<Vector3Int> results = new HashSet<Vector3Int>();

            HexFootprint.CollectCellsInRange(WedgeAt2_2, 0, results);

            Assert.That(results, Is.EquivalentTo(WedgeAt2_2));
        }

        [Test]
        public void CollectCellsInRange_NonEmptyResults_ClearsThemFirst()
        {
            HashSet<Vector3Int> results = new HashSet<Vector3Int> { Cell(20, 20), Cell(-20, -20) };

            HexFootprint.CollectCellsInRange(new[] { Cell(0, 0) }, 0, results);

            Assert.That(results, Is.EquivalentTo(new[] { Cell(0, 0) }));
        }
    }
}
