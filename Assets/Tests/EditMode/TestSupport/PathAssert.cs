using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SillyPirates.Tests.EditMode
{
    /// <summary>
    /// Structural assertions on a path returned by PathFindingUtils. These hold for any correct path
    /// regardless of how A* broke its ties, so they can be applied to cases whose exact route is not
    /// uniquely determined by the hex metric.
    /// </summary>
    internal static class PathAssert
    {
        /// <summary>Every consecutive pair of cells must be hex neighbours (distance 1).</summary>
        internal static void IsContiguous(IReadOnlyList<Vector3> path, Vector3Int start)
        {
            Vector3Int previous = start;
            for (int i = 0; i < path.Count; i++)
            {
                Vector3Int current = Vector3Int.RoundToInt(path[i]);

                // The first cell may be the start itself when includeStartingPosition is on.
                if (i == 0 && current == start)
                    continue;

                int step = PathFindingUtils.GetNormalizedDistance(previous, current);
                Assert.That(step, Is.EqualTo(1),
                    $"Cells {previous} and {current} (indices {i - 1}->{i}) are not adjacent.");

                previous = current;
            }
        }

        internal static void EndsAt(IReadOnlyList<Vector3> path, Vector3Int end)
        {
            Assert.That(path, Is.Not.Empty);
            Assert.That(Vector3Int.RoundToInt(path[path.Count - 1]), Is.EqualTo(end));
        }

        /// <summary>No cell of the path may be unwalkable (the starting cell is never part of the check).</summary>
        internal static void ContainsNoUnwalkableCell(IReadOnlyList<Vector3> path, TestGrid grid, Vector3Int start)
        {
            foreach (Vector3 cell in path)
            {
                Vector3Int pos = Vector3Int.RoundToInt(cell);
                if (pos == start)
                    continue;

                Assert.That(grid.IsWalkable(pos), Is.True, $"Path walks through the unwalkable cell {pos}.");
            }
        }

        /// <summary>Converts a path to integer cells for a straight equality assertion.</summary>
        internal static List<Vector3Int> ToCells(IReadOnlyList<Vector3> path)
        {
            List<Vector3Int> cells = new List<Vector3Int>(path.Count);
            foreach (Vector3 cell in path)
                cells.Add(Vector3Int.RoundToInt(cell));

            return cells;
        }
    }
}
