using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SillyPirates.Tests.EditMode.Grid.Utils
{
    /// <summary>
    /// Offset (odd-r, pointy-top) to cube conversions. The reference for adjacency is the pair of neighbour
    /// tables in PathFindingUtils, which predate HexCoordinates and drive every pathfinding test: the cube
    /// directions must land exactly on those cells, on both row parities, negative rows included.
    /// </summary>
    public class HexCoordinatesTests
    {
        private static Vector3Int Cell(int x, int y) => new Vector3Int(x, y, 0);

        private static readonly Vector3Int[] SampleCubes =
        {
            new Vector3Int(0, 0, 0),
            new Vector3Int(1, 0, -1),
            new Vector3Int(2, -1, -1),
            new Vector3Int(-3, 1, 2),
            new Vector3Int(4, -4, 0),
            new Vector3Int(-2, 5, -3),
        };

        [Test]
        public void CubeToOffset_OfOffsetToCube_ReturnsTheOriginalCellForEveryCellInRange()
        {
            List<string> mismatches = new List<string>();

            for (int y = -5; y <= 5; y++)
                for (int x = -5; x <= 5; x++)
                {
                    Vector3Int cell = Cell(x, y);
                    Vector3Int roundTrip = HexCoordinates.CubeToOffset(HexCoordinates.OffsetToCube(cell));
                    if (roundTrip != cell)
                        mismatches.Add($"{cell} -> {roundTrip}");
                }

            Assert.That(mismatches, Is.Empty);
        }

        [TestCase(0, -1)]
        [TestCase(3, -1)]
        [TestCase(-4, -3)]
        [TestCase(-1, -5)]
        public void CubeToOffset_OfOffsetToCube_NegativeOddRow_ReturnsTheOriginalCell(int x, int y)
        {
            Vector3Int cell = Cell(x, y);

            Vector3Int roundTrip = HexCoordinates.CubeToOffset(HexCoordinates.OffsetToCube(cell));

            Assert.That(roundTrip, Is.EqualTo(cell));
        }

        [Test]
        public void OffsetToCube_EveryCellInRange_ComponentsSumToZero()
        {
            List<string> violations = new List<string>();

            for (int y = -5; y <= 5; y++)
                for (int x = -5; x <= 5; x++)
                {
                    Vector3Int cube = HexCoordinates.OffsetToCube(Cell(x, y));
                    if (cube.x + cube.y + cube.z != 0)
                        violations.Add($"{Cell(x, y)} -> {cube}");
                }

            Assert.That(violations, Is.Empty);
        }

        /// <summary>The layer is documented as dropped: callers carry it over themselves.</summary>
        [Test]
        public void CubeToOffset_AnyCube_ReturnsZeroZ()
        {
            Vector3Int cell = HexCoordinates.CubeToOffset(HexCoordinates.OffsetToCube(new Vector3Int(2, 3, 7)));

            Assert.That(cell.z, Is.EqualTo(0));
        }

        /// <summary>
        /// Even row (2,2), odd row (3,3), and negative odd rows where y &amp; 1 relies on two's complement.
        /// </summary>
        [TestCase(2, 2)]
        [TestCase(3, 3)]
        [TestCase(0, 0)]
        [TestCase(0, -1)]
        [TestCase(-2, -3)]
        [TestCase(-1, -4)]
        public void DirectionToCube_SixDirectionsFromACell_EqualPathFindingNeighbors(int x, int y)
        {
            Vector3Int cell = Cell(x, y);
            Vector3Int cube = HexCoordinates.OffsetToCube(cell);
            List<Vector3Int> viaCube = new List<Vector3Int>();

            for (int d = 0; d < HexCoordinates.DirectionCount; d++)
                viaCube.Add(HexCoordinates.CubeToOffset(cube + HexCoordinates.DirectionToCube((HexDirection)d)));

            Assert.That(viaCube, Is.EquivalentTo(PathFindingUtils.GetNeighbors(cell)));
        }

        /// <summary>
        /// Pins the names: in odd-r with +y as north, East is x+1 on the same row and NorthEast from an even
        /// row is (x, y+1) (the row above is shifted right), from an odd row (x+1, y+1).
        /// </summary>
        [TestCase(2, 2, HexDirection.East, 3, 2)]
        [TestCase(2, 2, HexDirection.NorthEast, 2, 3)]
        [TestCase(2, 2, HexDirection.NorthWest, 1, 3)]
        [TestCase(2, 2, HexDirection.West, 1, 2)]
        [TestCase(2, 2, HexDirection.SouthWest, 1, 1)]
        [TestCase(2, 2, HexDirection.SouthEast, 2, 1)]
        [TestCase(2, 3, HexDirection.NorthEast, 3, 4)]
        [TestCase(2, 3, HexDirection.NorthWest, 2, 4)]
        [TestCase(2, 3, HexDirection.SouthWest, 2, 2)]
        [TestCase(2, 3, HexDirection.SouthEast, 3, 2)]
        public void DirectionToCube_NamedDirection_LandsOnTheExpectedOffsetCell(
            int x, int y, HexDirection direction, int expectedX, int expectedY)
        {
            Vector3Int cube = HexCoordinates.OffsetToCube(Cell(x, y)) + HexCoordinates.DirectionToCube(direction);

            Assert.That(HexCoordinates.CubeToOffset(cube), Is.EqualTo(Cell(expectedX, expectedY)));
        }

        [TestCase(6, HexDirection.East)]
        [TestCase(7, HexDirection.NorthEast)]
        [TestCase(-1, HexDirection.SouthEast)]
        [TestCase(-6, HexDirection.East)]
        public void DirectionToCube_OutOfRangeValue_Wraps(int raw, HexDirection equivalent)
        {
            Vector3Int wrapped = HexCoordinates.DirectionToCube((HexDirection)raw);

            Assert.That(wrapped, Is.EqualTo(HexCoordinates.DirectionToCube(equivalent)));
        }

        [Test]
        public void RotateCube_SixSteps_ReturnsTheStartingVector()
        {
            foreach (Vector3Int cube in SampleCubes)
                Assert.That(HexCoordinates.RotateCube(cube, 6), Is.EqualTo(cube), $"cube {cube}");
        }

        [Test]
        public void RotateCube_OneStepFromEast_ReturnsNorthEast()
        {
            Vector3Int east = HexCoordinates.DirectionToCube(HexDirection.East);

            Vector3Int rotated = HexCoordinates.RotateCube(east, 1);

            Assert.That(rotated, Is.EqualTo(HexCoordinates.DirectionToCube(HexDirection.NorthEast)));
        }

        [Test]
        public void RotateCube_OneStepFromNorthWest_ReturnsWest()
        {
            Vector3Int northWest = HexCoordinates.DirectionToCube(HexDirection.NorthWest);

            Vector3Int rotated = HexCoordinates.RotateCube(northWest, 1);

            Assert.That(rotated, Is.EqualTo(HexCoordinates.DirectionToCube(HexDirection.West)));
        }

        /// <summary>Rotating any direction k steps is the same as stepping k slots in the enum.</summary>
        [Test]
        public void RotateCube_AnyDirectionByK_EqualsDirectionPlusK()
        {
            for (int d = 0; d < HexCoordinates.DirectionCount; d++)
                for (int k = 0; k < HexCoordinates.DirectionCount; k++)
                {
                    Vector3Int rotated = HexCoordinates.RotateCube(HexCoordinates.DirectionToCube((HexDirection)d), k);

                    Assert.That(rotated, Is.EqualTo(HexCoordinates.DirectionToCube((HexDirection)((d + k) % 6))),
                        $"direction {(HexDirection)d} rotated {k}");
                }
        }

        [Test]
        public void RotateCube_MinusOneStep_EqualsFiveSteps()
        {
            foreach (Vector3Int cube in SampleCubes)
                Assert.That(HexCoordinates.RotateCube(cube, -1), Is.EqualTo(HexCoordinates.RotateCube(cube, 5)),
                    $"cube {cube}");
        }

        [Test]
        public void RotateCube_SevenSteps_EqualsOneStep()
        {
            foreach (Vector3Int cube in SampleCubes)
                Assert.That(HexCoordinates.RotateCube(cube, 7), Is.EqualTo(HexCoordinates.RotateCube(cube, 1)),
                    $"cube {cube}");
        }

        [Test]
        public void RotateCube_AnySteps_PreservesDistanceFromOrigin()
        {
            foreach (Vector3Int cube in SampleCubes)
                for (int steps = -7; steps <= 7; steps++)
                {
                    Vector3Int rotated = HexCoordinates.RotateCube(cube, steps);

                    Assert.That(HexCoordinates.CubeDistance(rotated, Vector3Int.zero),
                        Is.EqualTo(HexCoordinates.CubeDistance(cube, Vector3Int.zero)),
                        $"cube {cube} rotated {steps}");
                }
        }

        // ---------------------------------------------------------------- Neighbour

        [TestCase(2, 2, 5)]
        [TestCase(3, 3, -2)]
        [TestCase(0, -1, 1)]
        [TestCase(-2, -3, 4)]
        [TestCase(-1, -4, 0)]
        public void Neighbour_SixDirections_EqualPathFindingNeighborsKeepingZ(int x, int y, int z)
        {
            Vector3Int cell = new Vector3Int(x, y, z);
            List<Vector3Int> viaNeighbour = new List<Vector3Int>();

            for (int d = 0; d < HexCoordinates.DirectionCount; d++)
                viaNeighbour.Add(HexCoordinates.Neighbour(cell, (HexDirection)d));

            Assert.That(viaNeighbour, Is.EquivalentTo(PathFindingUtils.GetNeighbors(cell)));
        }

        [TestCase(2, 2, 7)]
        [TestCase(3, 3, -3)]
        [TestCase(-2, -3, 1)]
        public void Neighbour_AnyDirection_KeepsTheCellZ(int x, int y, int z)
        {
            Vector3Int cell = new Vector3Int(x, y, z);

            for (int d = 0; d < HexCoordinates.DirectionCount; d++)
                Assert.That(HexCoordinates.Neighbour(cell, (HexDirection)d).z, Is.EqualTo(z), $"{(HexDirection)d}");
        }

        /// <summary>
        /// The matching entry for each named direction, taken from PathFindingUtils' odd/even row tables
        /// (even row: NE = (0,+1), odd row: NE = (+1,+1)), on a non-zero layer.
        /// </summary>
        [TestCase(2, 2, HexDirection.East, 3, 2)]
        [TestCase(2, 2, HexDirection.NorthEast, 2, 3)]
        [TestCase(2, 2, HexDirection.NorthWest, 1, 3)]
        [TestCase(2, 2, HexDirection.West, 1, 2)]
        [TestCase(2, 2, HexDirection.SouthWest, 1, 1)]
        [TestCase(2, 2, HexDirection.SouthEast, 2, 1)]
        [TestCase(2, 3, HexDirection.East, 3, 3)]
        [TestCase(2, 3, HexDirection.NorthEast, 3, 4)]
        [TestCase(2, 3, HexDirection.NorthWest, 2, 4)]
        [TestCase(2, 3, HexDirection.West, 1, 3)]
        [TestCase(2, 3, HexDirection.SouthWest, 2, 2)]
        [TestCase(2, 3, HexDirection.SouthEast, 3, 2)]
        [TestCase(0, -1, HexDirection.NorthEast, 1, 0)]
        [TestCase(0, -1, HexDirection.SouthWest, 0, -2)]
        public void Neighbour_NamedDirection_ReturnsTheMatchingPathFindingNeighbour(
            int x, int y, HexDirection direction, int expectedX, int expectedY)
        {
            const int layer = 4;
            Vector3Int expected = new Vector3Int(expectedX, expectedY, layer);

            Vector3Int neighbour = HexCoordinates.Neighbour(new Vector3Int(x, y, layer), direction);

            Assert.That(neighbour, Is.EqualTo(expected));
            Assert.That(PathFindingUtils.GetNeighbors(new Vector3Int(x, y, layer)), Does.Contain(expected));
        }

        [Test]
        public void CubeDistance_EveryUnitDirection_IsOne()
        {
            for (int d = 0; d < HexCoordinates.DirectionCount; d++)
                Assert.That(HexCoordinates.CubeDistance(Vector3Int.zero, HexCoordinates.DirectionToCube((HexDirection)d)),
                    Is.EqualTo(1), $"direction {(HexDirection)d}");
        }
    }
}
