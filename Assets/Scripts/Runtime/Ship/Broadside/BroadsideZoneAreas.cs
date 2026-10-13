using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One Broadside area: a connected set of hex cells sharing the same firing priority.
/// </summary>
public readonly struct BroadsideZoneArea
{
    public readonly int Priority;
    public readonly IReadOnlyList<Vector3Int> Cells;

    public BroadsideZoneArea(int priority, IReadOnlyList<Vector3Int> cells)
    {
        Priority = priority;
        Cells = cells;
    }
}

/// <summary>
/// Pure grouping of Broadside zone cells into areas. Dependency-injected through delegates, like
/// <see cref="PathFindingUtils"/>, so it runs against a lambda grid with no Tilemap.
/// </summary>
public static class BroadsideZoneAreas
{
    /// <summary>
    /// Splits <paramref name="cells"/> into connected components (hex adjacency, odd-row offset, see
    /// <see cref="PathFindingUtils.GetNeighbors"/>) of cells with the same non-null priority. Two detached
    /// groups with the same priority are two areas. Cells whose priority is null are ignored. Deterministic:
    /// areas come out in the order their first cell appears in the input. <paramref name="results"/> is
    /// cleared first.
    /// </summary>
    public static void FindAreas(IEnumerable<Vector3Int> cells, Func<Vector3Int, int?> priorityOf, List<BroadsideZoneArea> results)
    {
        if (results == null) throw new ArgumentNullException(nameof(results));
        if (priorityOf == null) throw new ArgumentNullException(nameof(priorityOf));

        results.Clear();
        if (cells == null) return;

        var visited = new HashSet<Vector3Int>();
        var frontier = new Queue<Vector3Int>();

        foreach (Vector3Int start in cells)
        {
            if (visited.Contains(start)) continue;

            int? startPriority = priorityOf(start);
            if (!startPriority.HasValue) continue;

            int priority = startPriority.Value;
            var areaCells = new List<Vector3Int>();
            visited.Add(start);
            frontier.Enqueue(start);

            while (frontier.Count > 0)
            {
                Vector3Int current = frontier.Dequeue();
                areaCells.Add(current);

                List<Vector3Int> neighbors = PathFindingUtils.GetNeighbors(current);
                for (int i = 0; i < neighbors.Count; i++)
                {
                    Vector3Int neighbor = neighbors[i];
                    if (visited.Contains(neighbor)) continue;
                    if (priorityOf(neighbor) != priority) continue;

                    visited.Add(neighbor);
                    frontier.Enqueue(neighbor);
                }
            }

            results.Add(new BroadsideZoneArea(priority, areaCells));
        }
    }

    /// <summary>
    /// The plain average of the cells' world positions. It may fall outside a concave (L-shaped) area: that is
    /// intended. Returns <see cref="Vector3.zero"/> for an empty list.
    /// </summary>
    public static Vector3 Centroid(IReadOnlyList<Vector3Int> cells, Func<Vector3Int, Vector3> toWorld)
    {
        if (toWorld == null) throw new ArgumentNullException(nameof(toWorld));
        if (cells == null || cells.Count == 0) return Vector3.zero;

        Vector3 sum = Vector3.zero;
        for (int i = 0; i < cells.Count; i++)
            sum += toWorld(cells[i]);
        return sum / cells.Count;
    }
}
