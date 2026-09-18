using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SillyPirates.Tests.EditMode.Combat.Abilities.Shapes
{
    /// <summary>
    /// CircleShape is a stateless BFS ring expansion over PathFindingUtils.GetNeighbors.
    /// Expected values come from the hex spec: a filled hexagon of radius r holds 3r^2 + 3r + 1 cells
    /// (centered hexagonal numbers), and every cell it contains is exactly one within hex distance r.
    /// </summary>
    public class CircleShapeTests
    {
        private static Vector3Int Cell(int x, int y) => new Vector3Int(x, y, 0);

        private CircleShape _shape;

        [SetUp]
        public void SetUp() => _shape = new CircleShape();

        [TestCase(0, 1)]
        [TestCase(1, 7)]
        [TestCase(2, 19)]
        [TestCase(3, 37)]
        [TestCase(4, 61)]
        public void GetCells_KnownRange_ReturnsCenteredHexagonalCount(int range, int expectedCount)
        {
            List<Vector3> cells = _shape.GetCells(Cell(0, 0), range, Cell(0, 0));

            Assert.That(cells, Has.Count.EqualTo(expectedCount));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void GetCells_AnyRange_ReturnsDistinctCells(int range)
        {
            List<Vector3> cells = _shape.GetCells(Cell(0, 0), range, Cell(0, 0));

            Assert.That(cells, Is.Unique);
        }

        /// <summary>
        /// Soundness and completeness in one: the returned set must equal the set of every cell whose hex
        /// distance from the center is at most the range, brute-forced over a bounding box.
        /// </summary>
        [Test]
        public void GetCells_Range2_ReturnsExactlyTheCellsWithinHexDistanceTwo()
        {
            Vector3Int center = Cell(0, 0);
            const int range = 2;
            List<Vector3Int> expected = new List<Vector3Int>();
            for (int y = -range - 1; y <= range + 1; y++)
                for (int x = -range - 1; x <= range + 1; x++)
                    if (PathFindingUtils.GetNormalizedDistance(center, Cell(x, y)) <= range)
                        expected.Add(Cell(x, y));

            List<Vector3> cells = _shape.GetCells(center, range, center);

            Assert.That(PathAssert.ToCells(cells), Is.EquivalentTo(expected));
        }

        /// <summary>The ring expansion must not care about the parity of the center row.</summary>
        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(-3, -3)]
        [TestCase(5, -2)]
        public void GetCells_OddOrNegativeRowCenter_ReturnsSameCount(int cx, int cy)
        {
            List<Vector3> cells = _shape.GetCells(Cell(cx, cy), 2, Cell(0, 0));

            Assert.That(cells, Has.Count.EqualTo(19));
        }

        [Test]
        public void GetCells_AnyRange_ReturnsCenterAsFirstCell()
        {
            Vector3Int center = Cell(3, -2);

            List<Vector3> cells = _shape.GetCells(center, 3, Cell(0, 0));

            Assert.That(Vector3Int.RoundToInt(cells[0]), Is.EqualTo(center));
        }

        /// <summary>A circle is centred on the target, so the caster position must not influence it.</summary>
        [Test]
        public void GetCells_DifferentCasterPositions_ReturnsSameCells()
        {
            Vector3Int center = Cell(0, 0);

            List<Vector3> fromNear = _shape.GetCells(center, 2, Cell(1, 0));
            List<Vector3> fromFar = _shape.GetCells(center, 2, Cell(-7, 4));

            Assert.That(PathAssert.ToCells(fromFar), Is.EqualTo(PathAssert.ToCells(fromNear)));
        }

        /// <summary>
        /// Mandatory because ShapeFactory hands the same instance to every ability: a second call must not
        /// see anything left over from the first.
        /// </summary>
        [Test]
        public void GetCells_CalledTwiceOnSameInstance_ReturnsEqualResults()
        {
            Vector3Int center = Cell(2, 3);

            List<Vector3> first = _shape.GetCells(center, 2, Cell(0, 0));
            List<Vector3> second = _shape.GetCells(center, 2, Cell(0, 0));

            Assert.That(PathAssert.ToCells(second), Is.EqualTo(PathAssert.ToCells(first)));
        }
    }
}
