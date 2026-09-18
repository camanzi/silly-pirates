using System;
using System.Collections.Generic;
using UnityEngine;

namespace SillyPirates.Tests.EditMode
{
    /// <summary>
    /// A hex grid written inline in a test: a set of walkable cells plus an optional per-cell cost.
    ///
    /// PathFindingUtils is generic over TState and takes its walkability as a delegate, so this class is
    /// all the "grid" an A* test needs — no Tilemap, no scene, no TerrainTile assets.
    /// </summary>
    internal sealed class TestGrid
    {
        private readonly HashSet<Vector3Int> _walkable = new();
        private readonly Dictionary<Vector3Int, int> _costs = new();
        private int _defaultCost = 1;

        /// <summary>Matches Func&lt;Vector3Int, TState, bool&gt; with TState = TestGrid.</summary>
        internal static readonly Func<Vector3Int, TestGrid, bool> Walkability = (pos, grid) => grid.IsWalkable(pos);

        /// <summary>Every cell of a rectangle of offset coordinates is walkable (bounds inclusive).</summary>
        internal static TestGrid Rect(int minX, int maxX, int minY, int maxY)
        {
            TestGrid grid = new TestGrid();
            for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                    grid._walkable.Add(new Vector3Int(x, y, 0));

            return grid;
        }

        /// <summary>Only the listed cells are walkable. Used to force a single possible path.</summary>
        internal static TestGrid Cells(params Vector3Int[] walkable)
        {
            TestGrid grid = new TestGrid();
            foreach (Vector3Int cell in walkable)
                grid._walkable.Add(cell);

            return grid;
        }

        internal TestGrid Block(params Vector3Int[] cells)
        {
            foreach (Vector3Int cell in cells)
                _walkable.Remove(cell);

            return this;
        }

        internal TestGrid WithCost(Vector3Int cell, int cost)
        {
            _costs[cell] = cost;
            return this;
        }

        /// <summary>Cost applied to every cell with no explicit cost of its own.</summary>
        internal TestGrid WithUniformCost(int cost)
        {
            _defaultCost = cost;
            return this;
        }

        internal bool IsWalkable(Vector3Int pos) => _walkable.Contains(pos);

        /// <summary>Matches Func&lt;Vector3Int, int&gt;. Cells with no explicit cost use the uniform cost (1 by default).</summary>
        internal Func<Vector3Int, int> CostGetter =>
            pos => _costs.TryGetValue(pos, out int cost) ? cost : _defaultCost;
    }
}
