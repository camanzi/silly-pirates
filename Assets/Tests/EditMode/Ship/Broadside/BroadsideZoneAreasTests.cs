using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;

namespace SillyPirates.Tests.EditMode.Ship.Broadside
{
    /// <summary>
    /// Pins the contract of <see cref="BroadsideZoneAreas"/> as stated in its XML doc: connected components under
    /// hex adjacency (odd-row offset, <see cref="PathFindingUtils.GetNeighbors"/>) of cells sharing a non-null
    /// priority, areas in input order of their first cell, results cleared first; Centroid is the plain average.
    ///
    /// Adjacency expectations come from the PathFindingUtils tables:
    ///   odd row  (y odd):  (0,+1) (+1,+1) (+1,0) (+1,-1) (0,-1) (-1,0)  - diagonals reach x and x+1
    ///   even row (y even): (-1,+1) (0,+1) (+1,0) (0,-1) (-1,-1) (-1,0)  - diagonals reach x-1 and x
    /// The parity is read with (y &amp; 1), so y = -1 is odd.
    ///
    /// Cell order inside an area is BFS order, which the doc does not promise, so cells are compared as sets.
    /// </summary>
    public class BroadsideZoneAreasTests
    {
        private static Func<Vector3Int, int?> PriorityMap(Dictionary<Vector3Int, int> map) =>
            cell => map.TryGetValue(cell, out int priority) ? priority : (int?)null;

        private static Vector3Int C(int x, int y) => new Vector3Int(x, y, 0);

        // ---------- FindAreas: grouping ----------

        [Test]
        public void FindAreas_SingleCell_ReturnsOneAreaWithThatCellAndPriority()
        {
            var map = new Dictionary<Vector3Int, int> { [C(3, 4)] = 7 };
            var results = new List<BroadsideZoneArea>();

            BroadsideZoneAreas.FindAreas(map.Keys, PriorityMap(map), results);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Priority, Is.EqualTo(7));
            Assert.That(results[0].Cells, Is.EqualTo(new[] { C(3, 4) }));
        }

        [Test]
        public void FindAreas_ConnectedBlockSamePriority_ReturnsOneAreaWithAllCells()
        {
            var cells = new[] { C(0, 0), C(1, 0), C(2, 0), C(0, 1), C(1, 1), C(2, 1) };
            var map = new Dictionary<Vector3Int, int>();
            foreach (Vector3Int cell in cells) map[cell] = 1;
            var results = new List<BroadsideZoneArea>();

            BroadsideZoneAreas.FindAreas(cells, PriorityMap(map), results);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Cells, Is.EquivalentTo(cells));
        }

        [Test]
        public void FindAreas_TwoDetachedGroupsSamePriority_ReturnsTwoAreas()
        {
            var groupA = new[] { C(0, 0), C(1, 0) };
            var groupB = new[] { C(5, 0), C(6, 0) };
            var map = new Dictionary<Vector3Int, int>
            {
                [C(0, 0)] = 2, [C(1, 0)] = 2, [C(5, 0)] = 2, [C(6, 0)] = 2,
            };
            var results = new List<BroadsideZoneArea>();

            BroadsideZoneAreas.FindAreas(new[] { C(0, 0), C(1, 0), C(5, 0), C(6, 0) }, PriorityMap(map), results);

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results[0].Cells, Is.EquivalentTo(groupA));
            Assert.That(results[1].Cells, Is.EquivalentTo(groupB));
        }

        [Test]
        public void FindAreas_AdjacentCellsDifferentPriority_StaySeparateWithOwnPriority()
        {
            var map = new Dictionary<Vector3Int, int> { [C(0, 0)] = 1, [C(1, 0)] = 2 };
            var results = new List<BroadsideZoneArea>();

            BroadsideZoneAreas.FindAreas(new[] { C(0, 0), C(1, 0) }, PriorityMap(map), results);

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results[0].Priority, Is.EqualTo(1));
            Assert.That(results[0].Cells, Is.EqualTo(new[] { C(0, 0) }));
            Assert.That(results[1].Priority, Is.EqualTo(2));
            Assert.That(results[1].Cells, Is.EqualTo(new[] { C(1, 0) }));
        }

        // ---------- FindAreas: odd/even row adjacency ----------

        // Odd row y=1: upper diagonals go to x and x+1 -> (0,2) and (1,2); (-1,2) is not adjacent.
        [TestCase(0, 1, 0, 2, 1, TestName = "FindAreas_OddRow_UpperSameX_IsAdjacent")]
        [TestCase(0, 1, 1, 2, 1, TestName = "FindAreas_OddRow_UpperRightDiagonal_IsAdjacent")]
        [TestCase(0, 1, -1, 2, 2, TestName = "FindAreas_OddRow_UpperLeftDiagonal_IsNotAdjacent")]
        [TestCase(0, 1, 1, 0, 1, TestName = "FindAreas_OddRow_LowerRightDiagonal_IsAdjacent")]
        [TestCase(0, 1, -1, 0, 2, TestName = "FindAreas_OddRow_LowerLeftDiagonal_IsNotAdjacent")]
        // Even row y=2: diagonals go to x-1 and x -> (-1,3) and (0,3); (1,3) is not adjacent.
        [TestCase(0, 2, -1, 3, 1, TestName = "FindAreas_EvenRow_UpperLeftDiagonal_IsAdjacent")]
        [TestCase(0, 2, 0, 3, 1, TestName = "FindAreas_EvenRow_UpperSameX_IsAdjacent")]
        [TestCase(0, 2, 1, 3, 2, TestName = "FindAreas_EvenRow_UpperRightDiagonal_IsNotAdjacent")]
        [TestCase(0, 2, -1, 1, 1, TestName = "FindAreas_EvenRow_LowerLeftDiagonal_IsAdjacent")]
        [TestCase(0, 2, 1, 1, 2, TestName = "FindAreas_EvenRow_LowerRightDiagonal_IsNotAdjacent")]
        // Same row: horizontal neighbours on both parities; two steps apart is not adjacent.
        [TestCase(0, 0, 1, 0, 1, TestName = "FindAreas_EvenRow_Horizontal_IsAdjacent")]
        [TestCase(0, 1, -1, 1, 1, TestName = "FindAreas_OddRow_Horizontal_IsAdjacent")]
        [TestCase(0, 0, 2, 0, 2, TestName = "FindAreas_SameRowTwoApart_IsNotAdjacent")]
        // Negative odd row y=-1 ((-1 & 1) == 1): upper diagonals go to x and x+1.
        [TestCase(0, -1, 1, 0, 1, TestName = "FindAreas_NegativeOddRow_UpperRightDiagonal_IsAdjacent")]
        [TestCase(0, -1, -1, 0, 2, TestName = "FindAreas_NegativeOddRow_UpperLeftDiagonal_IsNotAdjacent")]
        public void FindAreas_TwoCellsSamePriority_AreaCountFollowsHexAdjacency(
            int startX, int startY, int otherX, int otherY, int expectedAreas)
        {
            Vector3Int start = C(startX, startY);
            Vector3Int other = C(otherX, otherY);
            var map = new Dictionary<Vector3Int, int> { [start] = 1, [other] = 1 };
            var results = new List<BroadsideZoneArea>();

            BroadsideZoneAreas.FindAreas(new[] { start, other }, PriorityMap(map), results);

            Assert.That(results.Count, Is.EqualTo(expectedAreas));
        }

        // ---------- FindAreas: null priority, empty/null input ----------

        [Test]
        public void FindAreas_CellWithNullPriority_ProducesNoArea()
        {
            var map = new Dictionary<Vector3Int, int> { [C(0, 0)] = 1 };
            var results = new List<BroadsideZoneArea>();

            BroadsideZoneAreas.FindAreas(new[] { C(4, 4), C(0, 0) }, PriorityMap(map), results);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Cells, Is.EqualTo(new[] { C(0, 0) }));
        }

        [Test]
        public void FindAreas_NullPriorityCellBetweenTwoCells_DoesNotBridgeThem()
        {
            // (0,0) - (1,0) - (2,0) in a row; the middle one has no priority.
            var map = new Dictionary<Vector3Int, int> { [C(0, 0)] = 1, [C(2, 0)] = 1 };
            var results = new List<BroadsideZoneArea>();

            BroadsideZoneAreas.FindAreas(new[] { C(0, 0), C(1, 0), C(2, 0) }, PriorityMap(map), results);

            Assert.That(results.Count, Is.EqualTo(2));
        }

        [Test]
        public void FindAreas_EmptyInput_ReturnsNoAreas()
        {
            var results = new List<BroadsideZoneArea>();

            BroadsideZoneAreas.FindAreas(Array.Empty<Vector3Int>(), _ => 1, results);

            Assert.That(results, Is.Empty);
        }

        [Test]
        public void FindAreas_NullCells_ReturnsNoAreas()
        {
            var results = new List<BroadsideZoneArea>();

            BroadsideZoneAreas.FindAreas(null, _ => 1, results);

            Assert.That(results, Is.Empty);
        }

        [Test]
        public void FindAreas_DuplicateCellInInput_ProducesOneArea()
        {
            var map = new Dictionary<Vector3Int, int> { [C(0, 0)] = 1 };
            var results = new List<BroadsideZoneArea>();

            BroadsideZoneAreas.FindAreas(new[] { C(0, 0), C(0, 0) }, PriorityMap(map), results);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Cells, Is.EqualTo(new[] { C(0, 0) }));
        }

        // ---------- FindAreas: ordering and output list ----------

        [Test]
        public void FindAreas_DetachedGroups_AreasFollowInputOrderOfFirstCell()
        {
            var map = new Dictionary<Vector3Int, int> { [C(0, 0)] = 1, [C(9, 0)] = 1, [C(5, 5)] = 1 };
            var results = new List<BroadsideZoneArea>();

            BroadsideZoneAreas.FindAreas(new[] { C(9, 0), C(5, 5), C(0, 0) }, PriorityMap(map), results);

            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results[0].Cells, Is.EqualTo(new[] { C(9, 0) }));
            Assert.That(results[1].Cells, Is.EqualTo(new[] { C(5, 5) }));
            Assert.That(results[2].Cells, Is.EqualTo(new[] { C(0, 0) }));
        }

        [Test]
        public void FindAreas_HigherPriorityFirstInInput_IsFirstArea()
        {
            var map = new Dictionary<Vector3Int, int> { [C(0, 0)] = 3, [C(1, 0)] = 1 };
            var results = new List<BroadsideZoneArea>();

            BroadsideZoneAreas.FindAreas(new[] { C(0, 0), C(1, 0) }, PriorityMap(map), results);

            Assert.That(results[0].Priority, Is.EqualTo(3));
        }

        [Test]
        public void FindAreas_ResultsAlreadyFilled_AreClearedFirst()
        {
            var map = new Dictionary<Vector3Int, int> { [C(0, 0)] = 1 };
            var stale = new BroadsideZoneArea(99, new[] { C(42, 42) });
            var results = new List<BroadsideZoneArea> { stale, stale };

            BroadsideZoneAreas.FindAreas(new[] { C(0, 0) }, PriorityMap(map), results);

            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0].Priority, Is.EqualTo(1));
        }

        [Test]
        public void FindAreas_NullCellsWithFilledResults_ClearsResults()
        {
            var results = new List<BroadsideZoneArea> { new BroadsideZoneArea(99, new[] { C(42, 42) }) };

            BroadsideZoneAreas.FindAreas(null, _ => 1, results);

            Assert.That(results, Is.Empty);
        }

        // ---------- FindAreas: argument validation ----------

        [Test]
        public void FindAreas_NullResults_ThrowsArgumentNullException()
        {
            Assert.That(() => BroadsideZoneAreas.FindAreas(new[] { C(0, 0) }, _ => 1, null),
                Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("results"));
        }

        [Test]
        public void FindAreas_NullPriorityOf_ThrowsArgumentNullException()
        {
            Assert.That(() => BroadsideZoneAreas.FindAreas(new[] { C(0, 0) }, null, new List<BroadsideZoneArea>()),
                Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("priorityOf"));
        }

        // ---------- Centroid ----------

        [Test]
        public void Centroid_ThreeCells_ReturnsAverageOfMappedPositions()
        {
            // toWorld: (x, y) -> (2x, 3y, 1). Cells (0,0) (2,0) (1,3) -> (0,0,1) (4,0,1) (2,9,1) -> avg (2,3,1).
            Func<Vector3Int, Vector3> toWorld = c => new Vector3(c.x * 2f, c.y * 3f, 1f);

            Vector3 centroid = BroadsideZoneAreas.Centroid(new[] { C(0, 0), C(2, 0), C(1, 3) }, toWorld);

            Assert.That(centroid, Is.EqualTo(new Vector3(2f, 3f, 1f)).Using(new Vector3EqualityComparer(1e-5f)));
        }

        [Test]
        public void Centroid_SingleCell_ReturnsThatCellsWorldPosition()
        {
            Func<Vector3Int, Vector3> toWorld = c => new Vector3(c.x + 0.5f, 7f, c.y - 0.25f);

            Vector3 centroid = BroadsideZoneAreas.Centroid(new[] { C(3, 4) }, toWorld);

            Assert.That(centroid, Is.EqualTo(new Vector3(3.5f, 7f, 3.75f)).Using(new Vector3EqualityComparer(1e-5f)));
        }

        [Test]
        public void Centroid_EmptyList_ReturnsZero()
        {
            Vector3 centroid = BroadsideZoneAreas.Centroid(Array.Empty<Vector3Int>(), c => Vector3.one);

            Assert.That(centroid, Is.EqualTo(Vector3.zero).Using(new Vector3EqualityComparer(1e-5f)));
        }

        [Test]
        public void Centroid_NullList_ReturnsZero()
        {
            Vector3 centroid = BroadsideZoneAreas.Centroid(null, c => Vector3.one);

            Assert.That(centroid, Is.EqualTo(Vector3.zero).Using(new Vector3EqualityComparer(1e-5f)));
        }

        [Test]
        public void Centroid_NullToWorld_ThrowsArgumentNullException()
        {
            Assert.That(() => BroadsideZoneAreas.Centroid(new[] { C(0, 0) }, null),
                Throws.TypeOf<ArgumentNullException>().With.Property("ParamName").EqualTo("toWorld"));
        }
    }
}
