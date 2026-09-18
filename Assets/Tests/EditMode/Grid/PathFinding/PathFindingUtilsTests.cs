using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SillyPirates.Tests.EditMode.Grid.PathFinding
{
    /// <summary>
    /// A* and hex-metric coverage for PathFindingUtils.
    ///
    /// Every expected value comes from the source formulas, not from a run:
    ///  - distances from OffsetToCube (q = x - (y - (y&amp;1))/2, odd-r offset) and the cube distance
    ///    (|dx|+|dy|+|dz|)/2, multiplied by 10 as GetDistance does;
    ///  - neighbour sets from the OddRowNeighbors / EvenRowNeighbors tables, cross-checked against the
    ///    metric (all six neighbours must sit at distance 1);
    ///  - path routes from the geometry, on grids built so the shortest route is the only route.
    /// </summary>
    public class PathFindingUtilsTests
    {
        private static Vector3Int Cell(int x, int y) => new Vector3Int(x, y, 0);

        // ---------------------------------------------------------------- GetNeighbors

        [Test]
        public void GetNeighbors_EvenRowCell_ReturnsEvenRowOffsets()
        {
            List<Vector3Int> neighbors = PathFindingUtils.GetNeighbors(Cell(0, 0));

            Assert.That(neighbors, Is.EquivalentTo(new[]
            {
                Cell(-1, 1), Cell(0, 1), Cell(1, 0), Cell(0, -1), Cell(-1, -1), Cell(-1, 0)
            }));
        }

        [Test]
        public void GetNeighbors_OddRowCell_ReturnsOddRowOffsets()
        {
            List<Vector3Int> neighbors = PathFindingUtils.GetNeighbors(Cell(0, 1));

            Assert.That(neighbors, Is.EquivalentTo(new[]
            {
                Cell(0, 2), Cell(1, 2), Cell(1, 1), Cell(1, 0), Cell(0, 0), Cell(-1, 1)
            }));
        }

        /// <summary>
        /// y = -1 must take the odd-row table: the source selects on (pos.y &amp; 1), and a bitwise AND
        /// classifies negative rows correctly where a modulo would not.
        /// </summary>
        [Test]
        public void GetNeighbors_NegativeOddRowCell_UsesOddRowTable()
        {
            List<Vector3Int> neighbors = PathFindingUtils.GetNeighbors(Cell(2, -1));

            Assert.That(neighbors, Is.EquivalentTo(new[]
            {
                Cell(2, 0), Cell(3, 0), Cell(3, -1), Cell(3, -2), Cell(2, -2), Cell(1, -1)
            }));
        }

        [Test]
        public void GetNeighbors_NegativeEvenRowCell_UsesEvenRowTable()
        {
            List<Vector3Int> neighbors = PathFindingUtils.GetNeighbors(Cell(2, -2));

            Assert.That(neighbors, Is.EquivalentTo(new[]
            {
                Cell(1, -1), Cell(2, -1), Cell(3, -2), Cell(2, -3), Cell(1, -3), Cell(1, -2)
            }));
        }

        /// <summary>
        /// Cross-check between the two neighbour tables and the cube metric: whatever the parity of the
        /// row, a cell has exactly six distinct neighbours and every one of them is at hex distance 1.
        /// </summary>
        [TestCase(0, 0)]
        [TestCase(0, 1)]
        [TestCase(2, -1)]
        [TestCase(2, -2)]
        [TestCase(-3, -3)]
        [TestCase(5, 4)]
        public void GetNeighbors_AnyCell_ReturnsSixDistinctCellsAllAtDistanceOne(int x, int y)
        {
            Vector3Int center = Cell(x, y);

            List<Vector3Int> neighbors = PathFindingUtils.GetNeighbors(center);

            Assert.That(neighbors, Has.Count.EqualTo(6));
            Assert.That(neighbors, Is.Unique);
            foreach (Vector3Int neighbor in neighbors)
                Assert.That(PathFindingUtils.GetNormalizedDistance(center, neighbor), Is.EqualTo(1),
                    $"{neighbor} is listed as a neighbour of {center} but is not at distance 1.");
        }

        // ---------------------------------------------------------------- GetDistance

        [TestCase(0, 0, 0, 0, 0)]
        [TestCase(0, 0, 1, 0, 10)]
        [TestCase(0, 0, 0, 1, 10)]
        [TestCase(0, 0, -1, 1, 10)]
        [TestCase(0, 0, 1, 1, 20)] // not a neighbour of an even row: the NE step from (0,0) is (0,1)
        [TestCase(0, 0, 0, 2, 20)]
        [TestCase(0, 0, 3, 0, 30)]
        [TestCase(0, 0, 0, -3, 30)]
        [TestCase(2, -1, 2, -3, 20)]
        [TestCase(-1, -1, 2, -4, 40)]
        public void GetDistance_KnownCellPairs_ReturnsHexDistanceTimesTen(int ax, int ay, int bx, int by, int expected)
        {
            int actual = PathFindingUtils.GetDistance(Cell(ax, ay), Cell(bx, by));

            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(0, 0, 1, 0)]
        [TestCase(0, 0, 1, 1)]
        [TestCase(0, 0, 0, -3)]
        [TestCase(2, -1, 2, -3)]
        [TestCase(-1, -1, 2, -4)]
        public void GetDistance_SwappedArguments_ReturnsSameValue(int ax, int ay, int bx, int by)
        {
            Vector3Int a = Cell(ax, ay);
            Vector3Int b = Cell(bx, by);

            Assert.That(PathFindingUtils.GetDistance(a, b), Is.EqualTo(PathFindingUtils.GetDistance(b, a)));
        }

        /// <summary>
        /// GetDistance is scaled by 10 and GetNormalizedDistance divides it back. The two APIs disagreeing
        /// is the classic source of off-by-ten range bugs, so the ratio is pinned explicitly.
        /// </summary>
        [TestCase(0, 0, 0, 0, 0)]
        [TestCase(0, 0, 1, 0, 1)]
        [TestCase(0, 0, 1, 1, 2)]
        [TestCase(0, 0, 3, 0, 3)]
        [TestCase(0, 0, 0, -3, 3)]
        [TestCase(-1, -1, 2, -4, 4)]
        public void GetNormalizedDistance_KnownCellPairs_ReturnsHexDistance(int ax, int ay, int bx, int by, int expected)
        {
            int actual = PathFindingUtils.GetNormalizedDistance(Cell(ax, ay), Cell(bx, by));

            Assert.That(actual, Is.EqualTo(expected));
        }

        // ---------------------------------------------------------------- FindPath

        [Test]
        public void FindPath_StartEqualsEnd_ReturnsEmptyPath()
        {
            TestGrid grid = TestGrid.Rect(-5, 5, -5, 5);

            List<Vector3> path = PathFindingUtils.FindPath(Cell(0, 0), Cell(0, 0), grid, TestGrid.Walkability);

            Assert.That(path, Is.Empty);
        }

        [Test]
        public void FindPath_StartEqualsEndAndIncludeStartingPosition_ReturnsOnlyStart()
        {
            TestGrid grid = TestGrid.Rect(-5, 5, -5, 5);

            List<Vector3> path = PathFindingUtils.FindPath(Cell(0, 0), Cell(0, 0), true, grid, TestGrid.Walkability);

            Assert.That(PathAssert.ToCells(path), Is.EqualTo(new[] { Cell(0, 0) }));
        }

        /// <summary>
        /// (0,0) -> (3,0) is three steps and only one three-step route exists: of the six neighbours of
        /// (0,0), only (1,0) sits at distance 2 from (3,0), and the same holds at every following step.
        /// </summary>
        [Test]
        public void FindPath_StraightRowOnOpenGrid_ReturnsShortestPathExcludingStart()
        {
            TestGrid grid = TestGrid.Rect(-5, 5, -5, 5);

            List<Vector3> path = PathFindingUtils.FindPath(Cell(0, 0), Cell(3, 0), grid, TestGrid.Walkability);

            Assert.That(PathAssert.ToCells(path), Is.EqualTo(new[] { Cell(1, 0), Cell(2, 0), Cell(3, 0) }));
        }

        [Test]
        public void FindPath_IncludeStartingPosition_PrependsStartCell()
        {
            TestGrid grid = TestGrid.Rect(-5, 5, -5, 5);

            List<Vector3> path = PathFindingUtils.FindPath(Cell(0, 0), Cell(3, 0), true, grid, TestGrid.Walkability);

            Assert.That(PathAssert.ToCells(path),
                Is.EqualTo(new[] { Cell(0, 0), Cell(1, 0), Cell(2, 0), Cell(3, 0) }));
        }

        /// <summary>
        /// Only a corridor is walkable, so the route is forced. Its length (4 steps) exceeds the straight
        /// hex distance between the endpoints (3), which is what makes this a genuine detour.
        /// </summary>
        [Test]
        public void FindPath_OnlyCorridorWalkable_ReturnsTheForcedDetour()
        {
            TestGrid grid = TestGrid.Cells(Cell(0, 0), Cell(1, 0), Cell(2, 0), Cell(2, 1), Cell(2, 2));

            List<Vector3> path = PathFindingUtils.FindPath(Cell(0, 0), Cell(2, 2), grid, TestGrid.Walkability);

            Assert.That(PathFindingUtils.GetNormalizedDistance(Cell(0, 0), Cell(2, 2)), Is.EqualTo(3),
                "Precondition: the straight-line distance must be shorter than the corridor.");
            Assert.That(PathAssert.ToCells(path),
                Is.EqualTo(new[] { Cell(1, 0), Cell(2, 0), Cell(2, 1), Cell(2, 2) }));
        }

        /// <summary>
        /// Unreachable target: the contract is an empty list, never null. Callers add to the returned list
        /// directly (MoveAbility.GetPreviewData), so null would NullReference far from here.
        /// </summary>
        [Test]
        public void FindPath_EndUnreachable_ReturnsEmptyListNotNull()
        {
            TestGrid grid = TestGrid.Rect(0, 2, 0, 0);

            List<Vector3> path = PathFindingUtils.FindPath(Cell(0, 0), Cell(5, 0), grid, TestGrid.Walkability);

            Assert.That(path, Is.Not.Null);
            Assert.That(path, Is.Empty);
        }

        /// <summary>
        /// The walkability check is applied to neighbours only, never to the starting cell. This is
        /// load-bearing, not an oversight: MoveAbility's predicate treats every occupied cell as
        /// unwalkable, and the caster is always standing on one.
        /// </summary>
        [Test]
        public void FindPath_StartCellNotWalkable_StillReturnsPath()
        {
            TestGrid grid = TestGrid.Rect(-5, 5, -5, 5).Block(Cell(0, 0));

            List<Vector3> path = PathFindingUtils.FindPath(Cell(0, 0), Cell(2, 0), grid, TestGrid.Walkability);

            Assert.That(PathAssert.ToCells(path), Is.EqualTo(new[] { Cell(1, 0), Cell(2, 0) }));
        }

        /// <summary>
        /// Cell costs must beat raw step count: the direct two-step route crosses a cost-10 cell
        /// (gCost 10*10 + 1*10 = 110) while the three-step detour costs 1 per cell (gCost 30).
        /// </summary>
        [Test]
        public void FindPath_ExpensiveDirectRouteAndCheapDetour_ReturnsCheaperDetour()
        {
            TestGrid grid = TestGrid
                .Cells(Cell(0, 0), Cell(1, 0), Cell(2, 0), Cell(0, -1), Cell(1, -1))
                .WithCost(Cell(1, 0), 10);

            List<Vector3> path = PathFindingUtils.FindPath(
                Cell(0, 0), Cell(2, 0), grid, TestGrid.Walkability, grid.CostGetter);

            Assert.That(PathAssert.ToCells(path),
                Is.EqualTo(new[] { Cell(0, -1), Cell(1, -1), Cell(2, 0) }));
        }

        /// <summary>
        /// FindPath exists twice, with the A* body copy-pasted: one adds a flat 10 per step, the other
        /// cellCostGetter(n) * 10. With every cell costing 1 the two are the same algorithm and must agree
        /// cell for cell. This is the guard against one copy being edited and the other not.
        /// </summary>
        [TestCase(0, 0, 3, 0)]
        [TestCase(0, 0, 2, 2)]
        [TestCase(-3, -2, 3, 2)]
        [TestCase(1, -3, -2, 3)]
        public void FindPath_UniformCellCostOfOne_MatchesFlatCostOverload(int sx, int sy, int ex, int ey)
        {
            TestGrid grid = TestGrid.Rect(-5, 5, -5, 5)
                .Block(Cell(1, 1), Cell(1, 0), Cell(1, -1), Cell(0, 2), Cell(-1, -2));
            Vector3Int start = Cell(sx, sy);
            Vector3Int end = Cell(ex, ey);

            List<Vector3> flat = PathFindingUtils.FindPath(start, end, grid, TestGrid.Walkability);
            List<Vector3> costed = PathFindingUtils.FindPath(start, end, grid, TestGrid.Walkability, grid.CostGetter);

            Assert.That(PathAssert.ToCells(costed), Is.EqualTo(PathAssert.ToCells(flat)));
        }

        [Test]
        public void FindPath_AnyResult_IsContiguousAndEndsAtTarget()
        {
            TestGrid grid = TestGrid.Rect(-5, 5, -5, 5)
                .Block(Cell(1, 1), Cell(1, 0), Cell(1, -1), Cell(1, -2), Cell(1, 2));
            Vector3Int start = Cell(0, 0);
            Vector3Int end = Cell(3, 0);

            List<Vector3> path = PathFindingUtils.FindPath(start, end, grid, TestGrid.Walkability);

            PathAssert.IsContiguous(path, start);
            PathAssert.EndsAt(path, end);
            PathAssert.ContainsNoUnwalkableCell(path, grid, start);
        }

        // ---------------------------------------------------------------- FindReachableArea

        [Test]
        public void FindReachableArea_MaxCostZero_ReturnsOnlyStartCell()
        {
            TestGrid grid = TestGrid.Rect(-5, 5, -5, 5);

            List<Vector3> area = PathFindingUtils.FindReachableArea(Cell(0, 0), 0, grid, TestGrid.Walkability);

            Assert.That(PathAssert.ToCells(area), Is.EqualTo(new[] { Cell(0, 0) }));
        }

        /// <summary>
        /// Distinctness is the real guard here. The search keeps no closed set, so if the "pop the cheapest
        /// open node" scan were ever replaced by a FIFO the same cell could be emitted twice and every
        /// caller counting cells would silently over-count.
        /// </summary>
        [Test]
        public void FindReachableArea_OpenGridRange1_ReturnsSevenDistinctCells()
        {
            TestGrid grid = TestGrid.Rect(-6, 6, -6, 6);

            List<Vector3> area = PathFindingUtils.FindReachableArea(Cell(0, 0), 1, grid, TestGrid.Walkability);

            Assert.That(area, Has.Count.EqualTo(7));
            Assert.That(PathAssert.ToCells(area), Is.Unique);
        }

        /// <summary>Centered hexagonal numbers: a full hex of radius r holds 3r^2 + 3r + 1 cells.</summary>
        [TestCase(0, 1)]
        [TestCase(1, 7)]
        [TestCase(2, 19)]
        [TestCase(3, 37)]
        public void FindReachableArea_OpenGridRange_ReturnsCenteredHexagonalCount(int range, int expectedCount)
        {
            TestGrid grid = TestGrid.Rect(-8, 8, -8, 8);

            List<Vector3> area = PathFindingUtils.FindReachableArea(Cell(0, 0), range, grid, TestGrid.Walkability);

            Assert.That(area, Has.Count.EqualTo(expectedCount));
        }

        [Test]
        public void FindReachableArea_AnyResult_ContainsOnlyCellsWithinRange()
        {
            TestGrid grid = TestGrid.Rect(-8, 8, -8, 8);
            Vector3Int start = Cell(0, 0);

            List<Vector3> area = PathFindingUtils.FindReachableArea(start, 3, grid, TestGrid.Walkability);

            foreach (Vector3Int cell in PathAssert.ToCells(area))
                Assert.That(PathFindingUtils.GetNormalizedDistance(start, cell), Is.LessThanOrEqualTo(3),
                    $"{cell} is further than 3 steps from {start}.");
        }

        [Test]
        public void FindReachableArea_StartSurroundedByUnwalkableCells_ReturnsOnlyStart()
        {
            TestGrid grid = TestGrid.Cells(Cell(0, 0));

            List<Vector3> area = PathFindingUtils.FindReachableArea(Cell(0, 0), 3, grid, TestGrid.Walkability);

            Assert.That(PathAssert.ToCells(area), Is.EqualTo(new[] { Cell(0, 0) }));
        }

        /// <summary>
        /// Same omission as FindPath, same reason: the starting cell is never walkability-checked, and
        /// MoveAbility depends on it because the caster's own cell always reads as occupied.
        /// </summary>
        [Test]
        public void FindReachableArea_StartCellNotWalkable_StillIncludesStart()
        {
            TestGrid grid = TestGrid.Cells(); // nothing at all is walkable

            List<Vector3> area = PathFindingUtils.FindReachableArea(Cell(0, 0), 2, grid, TestGrid.Walkability);

            Assert.That(PathAssert.ToCells(area), Is.EqualTo(new[] { Cell(0, 0) }));
        }

        /// <summary>
        /// The cellCostGetter overload adds the raw cell cost (not cost * 10, unlike FindPath), so with
        /// every cell costing 2 a budget of 2 buys the first ring only: 1 + 6 cells.
        /// </summary>
        [Test]
        public void FindReachableArea_CellCostGetterReturningTwo_ReachesFirstRingOnly()
        {
            TestGrid grid = TestGrid.Rect(-6, 6, -6, 6).WithUniformCost(2);

            List<Vector3> area = PathFindingUtils.FindReachableArea(
                Cell(0, 0), 2, grid, TestGrid.Walkability, grid.CostGetter);

            Assert.That(area, Has.Count.EqualTo(7));
            Assert.That(PathAssert.ToCells(area), Is.Unique);
        }

        /// <summary>
        /// FindReachableArea is also duplicated. The flat overload hard-codes a step cost of 1, so with a
        /// cost getter returning 1 the two must agree on the same cells.
        /// </summary>
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void FindReachableArea_CellCostGetterReturningOne_MatchesUniformCostOverload(int range)
        {
            TestGrid grid = TestGrid.Rect(-8, 8, -8, 8).Block(Cell(1, 0), Cell(0, 1), Cell(-1, -1));
            Vector3Int start = Cell(0, 0);

            List<Vector3> flat = PathFindingUtils.FindReachableArea(start, range, grid, TestGrid.Walkability);
            List<Vector3> costed = PathFindingUtils.FindReachableArea(
                start, range, grid, TestGrid.Walkability, grid.CostGetter);

            Assert.That(PathAssert.ToCells(costed), Is.EquivalentTo(PathAssert.ToCells(flat)));
        }

        [Test]
        public void FindReachableArea_AnyResult_ContainsNoUnwalkableCellExceptStart()
        {
            TestGrid grid = TestGrid.Rect(-6, 6, -6, 6).Block(Cell(1, 0), Cell(0, 1), Cell(-1, 1));
            Vector3Int start = Cell(0, 0);

            List<Vector3> area = PathFindingUtils.FindReachableArea(start, 3, grid, TestGrid.Walkability);

            foreach (Vector3Int cell in PathAssert.ToCells(area))
            {
                if (cell == start) continue;
                Assert.That(grid.IsWalkable(cell), Is.True, $"{cell} is unwalkable but was reported reachable.");
            }
        }
    }
}
