using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SillyPirates.Tests.EditMode.Grid.Footprint
{
    /// <summary>
    /// Footprint geometry. Exact expectations are derived from the odd-r neighbour tables (the same ones
    /// hard-coded in PathFindingUtils), written out below as a direction-indexed table so the expected cells
    /// do not go through HexCoordinates, which is what Resolve itself uses.
    /// </summary>
    public class HexFootprintTests
    {
        private static Vector3Int Cell(int x, int y, int z = 0) => new Vector3Int(x, y, z);

        // Offset step per HexDirection (East, NorthEast, NorthWest, West, SouthWest, SouthEast), odd-r layout.
        private static readonly Vector3Int[] EvenRowSteps =
        {
            Cell(1, 0), Cell(0, 1), Cell(-1, 1), Cell(-1, 0), Cell(-1, -1), Cell(0, -1),
        };

        private static readonly Vector3Int[] OddRowSteps =
        {
            Cell(1, 0), Cell(1, 1), Cell(0, 1), Cell(-1, 0), Cell(0, -1), Cell(1, -1),
        };

        private static Vector3Int Step(Vector3Int from, HexDirection direction)
        {
            Vector3Int[] table = (from.y & 1) != 0 ? OddRowSteps : EvenRowSteps;
            return from + table[(int)direction];
        }

        private static HexDirection Opposite(HexDirection direction) => (HexDirection)(((int)direction + 3) % 6);

        // Non-zero z on purpose: the layer must survive the cube round trip.
        private static readonly Vector3Int[] Anchors =
        {
            Cell(2, 4, 3),   // even row
            Cell(5, 1, 3),   // odd row
            Cell(-3, -3, -2) // negative odd row
        };

        private static readonly FootprintShape[] Shapes =
        {
            FootprintShape.Single, FootprintShape.Line, FootprintShape.Cluster,
        };

        private struct Case
        {
            public Vector3Int Anchor;
            public FootprintShape Shape;
            public int Count;
            public HexDirection Direction;

            public override string ToString() => $"{Shape} N={Count} {Direction} at {Anchor}";
        }

        private static int MaxCount(FootprintShape shape) => shape switch
        {
            FootprintShape.Line => HexFootprint.MaxLineCount,
            FootprintShape.Cluster => HexFootprint.MaxClusterCount,
            _ => 1,
        };

        private static IEnumerable<Case> AllValidCases()
        {
            foreach (FootprintShape shape in Shapes)
                for (int n = 1; n <= MaxCount(shape); n++)
                    for (int d = 0; d < 6; d++)
                        foreach (Vector3Int anchor in Anchors)
                            yield return new Case { Anchor = anchor, Shape = shape, Count = n, Direction = (HexDirection)d };
        }

        private static List<Vector3Int> Resolve(Vector3Int anchor, FootprintShape shape, int count, HexDirection direction)
        {
            List<Vector3Int> results = new List<Vector3Int>();
            HexFootprint.Resolve(anchor, shape, count, direction, results);
            return results;
        }

        private static List<Vector3Int> Resolve(Case c) => Resolve(c.Anchor, c.Shape, c.Count, c.Direction);

        // ---------------------------------------------------------------- invariants

        [Test]
        public void Resolve_EveryValidCase_FirstCellIsTheAnchor()
        {
            foreach (Case c in AllValidCases())
                Assert.That(Resolve(c)[0], Is.EqualTo(c.Anchor), c.ToString());
        }

        [Test]
        public void Resolve_EveryValidCase_HasNoDuplicates()
        {
            foreach (Case c in AllValidCases())
                Assert.That(Resolve(c), Is.Unique, c.ToString());
        }

        [Test]
        public void Resolve_EveryValidCase_CountEqualsClampCount()
        {
            foreach (Case c in AllValidCases())
                Assert.That(Resolve(c), Has.Count.EqualTo(HexFootprint.ClampCount(c.Shape, c.Count)), c.ToString());
        }

        [Test]
        public void Resolve_EveryValidCase_CellsAreConnected()
        {
            foreach (Case c in AllValidCases())
            {
                List<Vector3Int> cells = Resolve(c);
                HashSet<Vector3Int> members = new HashSet<Vector3Int>(cells);
                HashSet<Vector3Int> reached = new HashSet<Vector3Int> { cells[0] };
                Queue<Vector3Int> queue = new Queue<Vector3Int>();
                queue.Enqueue(cells[0]);

                while (queue.Count > 0)
                    foreach (Vector3Int nb in PathFindingUtils.GetNeighbors(queue.Dequeue()))
                        if (members.Contains(nb) && reached.Add(nb))
                            queue.Enqueue(nb);

                Assert.That(reached, Has.Count.EqualTo(members.Count), c.ToString());
            }
        }

        [Test]
        public void Resolve_EveryValidCase_EveryCellKeepsAnchorZ()
        {
            foreach (Case c in AllValidCases())
                foreach (Vector3Int cell in Resolve(c))
                    Assert.That(cell.z, Is.EqualTo(c.Anchor.z), $"{c}: cell {cell}");
        }

        // ---------------------------------------------------------------- Single

        [Test]
        public void Resolve_Single_IgnoresCountAndDirection()
        {
            Vector3Int anchor = Cell(2, 4, 3);

            foreach (int n in new[] { -1, 0, 1, 3, 9, 50 })
                for (int d = 0; d < 6; d++)
                    Assert.That(Resolve(anchor, FootprintShape.Single, n, (HexDirection)d),
                        Is.EqualTo(new[] { anchor }), $"N={n} {(HexDirection)d}");
        }

        // ---------------------------------------------------------------- Line, exact cases

        [Test]
        public void Resolve_Line3EastAtEvenRow_ReturnsTheRowCentredOnTheAnchor()
        {
            List<Vector3Int> cells = Resolve(Cell(2, 2), FootprintShape.Line, 3, HexDirection.East);

            Assert.That(cells, Is.EquivalentTo(new[] { Cell(1, 2), Cell(2, 2), Cell(3, 2) }));
        }

        /// <summary>From even row (2,2): NorthEast is (2,3), SouthWest is (1,1).</summary>
        [Test]
        public void Resolve_Line3NorthEastAtEvenRow_ReturnsTheDiagonalCentredOnTheAnchor()
        {
            List<Vector3Int> cells = Resolve(Cell(2, 2), FootprintShape.Line, 3, HexDirection.NorthEast);

            Assert.That(cells, Is.EquivalentTo(new[] { Cell(1, 1), Cell(2, 2), Cell(2, 3) }));
        }

        /// <summary>From odd row (2,3): NorthEast is (3,4), SouthWest is (2,2).</summary>
        [Test]
        public void Resolve_Line3NorthEastAtOddRow_ReturnsTheDiagonalCentredOnTheAnchor()
        {
            List<Vector3Int> cells = Resolve(Cell(2, 3), FootprintShape.Line, 3, HexDirection.NorthEast);

            Assert.That(cells, Is.EquivalentTo(new[] { Cell(2, 2), Cell(2, 3), Cell(3, 4) }));
        }

        [Test]
        public void Resolve_Line2_ReturnsAnchorAndItsNeighbourInTheDirection()
        {
            foreach (Vector3Int anchor in Anchors)
                for (int d = 0; d < 6; d++)
                {
                    HexDirection direction = (HexDirection)d;

                    List<Vector3Int> cells = Resolve(anchor, FootprintShape.Line, 2, direction);

                    Assert.That(cells, Is.EquivalentTo(new[] { anchor, Step(anchor, direction) }),
                        $"{direction} at {anchor}");
                }
        }

        [Test]
        public void Resolve_Line4_HasOneCellBehindAndTwoAhead()
        {
            foreach (Vector3Int anchor in Anchors)
                for (int d = 0; d < 6; d++)
                {
                    HexDirection direction = (HexDirection)d;
                    Vector3Int ahead1 = Step(anchor, direction);
                    Vector3Int[] expected =
                    {
                        Step(anchor, Opposite(direction)), anchor, ahead1, Step(ahead1, direction),
                    };

                    List<Vector3Int> cells = Resolve(anchor, FootprintShape.Line, 4, direction);

                    Assert.That(cells, Is.EquivalentTo(expected), $"{direction} at {anchor}");
                }
        }

        [Test]
        public void Resolve_Line4NorthEastAtEvenRow_ReturnsExpectedCells()
        {
            List<Vector3Int> cells = Resolve(Cell(2, 2), FootprintShape.Line, 4, HexDirection.NorthEast);

            Assert.That(cells, Is.EquivalentTo(new[] { Cell(1, 1), Cell(2, 2), Cell(2, 3), Cell(3, 4) }));
        }

        [Test]
        public void Resolve_LineOddCount_OppositeDirectionsGiveTheSameCells()
        {
            foreach (Vector3Int anchor in Anchors)
                for (int n = 1; n <= HexFootprint.MaxLineCount; n += 2)
                    for (int d = 0; d < 3; d++)
                    {
                        List<Vector3Int> forward = Resolve(anchor, FootprintShape.Line, n, (HexDirection)d);
                        List<Vector3Int> backward = Resolve(anchor, FootprintShape.Line, n, (HexDirection)(d + 3));

                        Assert.That(backward, Is.EquivalentTo(forward), $"N={n} {(HexDirection)d} at {anchor}");
                    }
        }

        [Test]
        public void Resolve_LineEvenCount_OppositeDirectionsGiveDifferentCells()
        {
            foreach (Vector3Int anchor in Anchors)
                for (int n = 2; n <= HexFootprint.MaxLineCount; n += 2)
                    for (int d = 0; d < 3; d++)
                    {
                        List<Vector3Int> forward = Resolve(anchor, FootprintShape.Line, n, (HexDirection)d);
                        List<Vector3Int> backward = Resolve(anchor, FootprintShape.Line, n, (HexDirection)(d + 3));

                        Assert.That(backward, Is.Not.EquivalentTo(forward), $"N={n} {(HexDirection)d} at {anchor}");
                    }
        }

        [Test]
        public void Resolve_LineAnyCount_EveryCellWithinHalfCountOfTheAnchor()
        {
            foreach (Vector3Int anchor in Anchors)
                for (int n = 1; n <= HexFootprint.MaxLineCount; n++)
                    for (int d = 0; d < 6; d++)
                        foreach (Vector3Int cell in Resolve(anchor, FootprintShape.Line, n, (HexDirection)d))
                            Assert.That(PathFindingUtils.GetNormalizedDistance(anchor, cell), Is.LessThanOrEqualTo(n / 2),
                                $"N={n} {(HexDirection)d} at {anchor}: cell {cell}");
        }

        // ---------------------------------------------------------------- Cluster

        [Test]
        public void Resolve_Cluster7_EqualsAnchorPlusAllNeighbours()
        {
            foreach (Vector3Int anchor in Anchors)
                for (int d = 0; d < 6; d++)
                {
                    List<Vector3Int> expected = PathFindingUtils.GetNeighbors(anchor);
                    expected.Add(anchor);

                    List<Vector3Int> cells = Resolve(anchor, FootprintShape.Cluster, 7, (HexDirection)d);

                    Assert.That(cells, Is.EquivalentTo(expected), $"{(HexDirection)d} at {anchor}");
                }
        }

        [Test]
        public void Resolve_Cluster3East_ReturnsAnchorEastAndNorthEast()
        {
            foreach (Vector3Int anchor in Anchors)
            {
                Vector3Int[] expected =
                {
                    anchor, Step(anchor, HexDirection.East), Step(anchor, HexDirection.NorthEast),
                };

                List<Vector3Int> cells = Resolve(anchor, FootprintShape.Cluster, 3, HexDirection.East);

                Assert.That(cells, Is.EquivalentTo(expected), $"at {anchor}");
            }
        }

        // ---------------------------------------------------------------- rotation

        /// <summary>Resolve(direction k) must be Resolve(East) turned k steps counter-clockwise around the anchor.</summary>
        [TestCase(FootprintShape.Line)]
        [TestCase(FootprintShape.Cluster)]
        public void Resolve_DirectionK_EqualsEastRotatedKStepsAroundTheAnchor(FootprintShape shape)
        {
            foreach (Vector3Int anchor in Anchors)
                for (int n = 1; n <= MaxCount(shape); n++)
                {
                    Vector3Int anchorCube = HexCoordinates.OffsetToCube(anchor);
                    List<Vector3Int> east = Resolve(anchor, shape, n, HexDirection.East);

                    for (int k = 0; k < 6; k++)
                    {
                        List<Vector3Int> expected = new List<Vector3Int>();
                        foreach (Vector3Int cell in east)
                        {
                            Vector3Int rotated = anchorCube +
                                HexCoordinates.RotateCube(HexCoordinates.OffsetToCube(cell) - anchorCube, k);
                            Vector3Int back = HexCoordinates.CubeToOffset(rotated);
                            back.z = anchor.z;
                            expected.Add(back);
                        }

                        List<Vector3Int> actual = Resolve(anchor, shape, n, (HexDirection)k);

                        Assert.That(actual, Is.EquivalentTo(expected), $"{shape} N={n} k={k} at {anchor}");
                    }
                }
        }

        // ---------------------------------------------------------------- clamping

        [TestCase(FootprintShape.Single, 5, 1)]
        [TestCase(FootprintShape.Line, 0, 1)]
        [TestCase(FootprintShape.Line, -3, 1)]
        [TestCase(FootprintShape.Line, 50, 9)]
        [TestCase(FootprintShape.Line, 9, 9)]
        [TestCase(FootprintShape.Cluster, 0, 1)]
        [TestCase(FootprintShape.Cluster, -3, 1)]
        [TestCase(FootprintShape.Cluster, 8, 7)]
        [TestCase(FootprintShape.Cluster, 7, 7)]
        public void ClampCount_OutOfOrInRange_ReturnsLegalCount(FootprintShape shape, int count, int expected)
        {
            Assert.That(HexFootprint.ClampCount(shape, count), Is.EqualTo(expected));
        }

        [TestCase(FootprintShape.Line, 0, 1)]
        [TestCase(FootprintShape.Line, -3, 1)]
        [TestCase(FootprintShape.Line, 50, 9)]
        [TestCase(FootprintShape.Cluster, 0, 1)]
        [TestCase(FootprintShape.Cluster, -3, 1)]
        [TestCase(FootprintShape.Cluster, 8, 7)]
        public void Resolve_OutOfRangeCount_ResolvesTheClampedNumberOfCells(FootprintShape shape, int count, int expected)
        {
            List<Vector3Int> cells = Resolve(Cell(2, 2), shape, count, HexDirection.East);

            Assert.That(cells, Has.Count.EqualTo(expected));
        }

        [Test]
        public void GridFootprint_Count_ReturnsClampedValue()
        {
            GridFootprint footprint = new GridFootprint(FootprintShape.Cluster, 8, HexDirection.East);

            Assert.That(footprint.Count, Is.EqualTo(7));
        }

        [Test]
        public void Resolve_GridFootprintOverload_MatchesExplicitOverload()
        {
            GridFootprint footprint = new GridFootprint(FootprintShape.Line, 4, HexDirection.SouthWest);
            List<Vector3Int> viaStruct = new List<Vector3Int>();

            HexFootprint.Resolve(Cell(5, 1, 3), footprint, viaStruct);

            Assert.That(viaStruct, Is.EqualTo(Resolve(Cell(5, 1, 3), FootprintShape.Line, 4, HexDirection.SouthWest)));
        }

        [Test]
        public void Resolve_NonEmptyResultsList_ClearsItFirst()
        {
            List<Vector3Int> results = new List<Vector3Int> { Cell(9, 9), Cell(8, 8), Cell(7, 7) };

            HexFootprint.Resolve(Cell(2, 2), FootprintShape.Single, 1, HexDirection.East, results);

            Assert.That(results, Is.EqualTo(new[] { Cell(2, 2) }));
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
            List<Vector3Int> cells = Resolve(Cell(2, 2), FootprintShape.Line, 3, HexDirection.East);

            int distance = HexFootprint.DistanceTo(cells, Cell(3, 2));

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
            Vector3Int[] cells = { Cell(1, 2), Cell(2, 2), Cell(3, 2) };
            HashSet<Vector3Int> set = new HashSet<Vector3Int> { Cell(9, 9), Cell(3, 2) };

            Assert.That(HexFootprint.Intersects(cells, set), Is.True);
        }

        [Test]
        public void Intersects_DisjointSets_ReturnsFalse()
        {
            Vector3Int[] cells = { Cell(1, 2), Cell(2, 2), Cell(3, 2) };
            HashSet<Vector3Int> set = new HashSet<Vector3Int> { Cell(4, 2), Cell(2, 3) };

            Assert.That(HexFootprint.Intersects(cells, set), Is.False);
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
            List<Vector3Int> cells = Resolve(Cell(2, 3), FootprintShape.Line, 3, HexDirection.NorthEast);
            HashSet<Vector3Int> expected = new HashSet<Vector3Int>();
            CircleShape circle = new CircleShape();
            foreach (Vector3Int cell in cells)
                expected.UnionWith(PathAssert.ToCells(circle.GetCells(cell, range, cell)));
            HashSet<Vector3Int> results = new HashSet<Vector3Int>();

            HexFootprint.CollectCellsInRange(cells, range, results);

            Assert.That(results, Is.EquivalentTo(expected));
        }

        [Test]
        public void CollectCellsInRange_RangeZero_ReturnsTheFootprintItself()
        {
            List<Vector3Int> cells = Resolve(Cell(2, 2), FootprintShape.Cluster, 4, HexDirection.West);
            HashSet<Vector3Int> results = new HashSet<Vector3Int>();

            HexFootprint.CollectCellsInRange(cells, 0, results);

            Assert.That(results, Is.EquivalentTo(cells));
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
