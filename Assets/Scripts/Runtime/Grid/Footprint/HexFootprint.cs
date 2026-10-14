using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure footprint geometry: turns a <see cref="GridFootprint"/> into concrete offset cells and answers
/// distance/overlap questions about a set of cells. No Unity objects involved, so it is EditMode-testable.
/// </summary>
public static class HexFootprint
{
    public const int MaxLineCount = 9;
    public const int MaxClusterCount = 7;

    /// <summary>
    /// The legal number of cells for <paramref name="shape"/>: Single is always 1, Line 1..9, Cluster 1..7.
    /// Zero or negative counts (e.g. a prefab saved before the field existed) become 1.
    /// </summary>
    public static int ClampCount(FootprintShape shape, int count)
    {
        switch (shape)
        {
            case FootprintShape.Line: return Mathf.Clamp(count, 1, MaxLineCount);
            case FootprintShape.Cluster: return Mathf.Clamp(count, 1, MaxClusterCount);
            default: return 1;
        }
    }

    public static void Resolve(Vector3Int anchor, GridFootprint footprint, List<Vector3Int> results)
        => Resolve(anchor, footprint.Shape, footprint.Count, footprint.Direction, results);

    /// <summary>
    /// Clears <paramref name="results"/> and fills it with the cells covered by the footprint.
    /// Guarantees: <c>results[0] == anchor</c>, no duplicates, the cells are connected, and every cell keeps
    /// <c>anchor.z</c>. Unknown shapes resolve as Single.
    /// </summary>
    public static void Resolve(Vector3Int anchor, FootprintShape shape, int count, HexDirection direction,
                               List<Vector3Int> results)
    {
        results.Clear();
        results.Add(anchor);

        int n = ClampCount(shape, count);
        if (n <= 1) return;

        Vector3Int anchorCube = HexCoordinates.OffsetToCube(anchor);

        switch (shape)
        {
            case FootprintShape.Line:
            {
                // Centred on the anchor; with an even count the extra cell goes toward the direction.
                Vector3Int step = HexCoordinates.DirectionToCube(direction);
                int from = -(n - 1) / 2;
                int to = n / 2;
                for (int k = from; k <= to; k++)
                {
                    if (k == 0) continue;
                    results.Add(CubeToCell(anchorCube + step * k, anchor.z));
                }
                break;
            }
            case FootprintShape.Cluster:
            {
                // The hub plus its neighbours, counter-clockwise starting from the direction:
                // 3 is a triangle, 7 the full hex.
                for (int i = 0; i < n - 1; i++)
                {
                    Vector3Int dir = HexCoordinates.DirectionToCube((HexDirection)((int)direction + i));
                    results.Add(CubeToCell(anchorCube + dir, anchor.z));
                }
                break;
            }
        }
    }

    /// <summary>
    /// The smallest hex distance (in cells, like <see cref="PathFindingUtils.GetNormalizedDistance"/>) from
    /// any of <paramref name="cells"/> to <paramref name="target"/>. <see cref="int.MaxValue"/> when empty.
    /// </summary>
    public static int DistanceTo(IReadOnlyList<Vector3Int> cells, Vector3Int target)
    {
        int best = int.MaxValue;
        Vector3Int targetCube = HexCoordinates.OffsetToCube(target);
        for (int i = 0; i < cells.Count; i++)
        {
            int d = HexCoordinates.CubeDistance(HexCoordinates.OffsetToCube(cells[i]), targetCube);
            if (d < best) best = d;
        }
        return best;
    }

    /// <summary>True when at least one of <paramref name="cells"/> is in <paramref name="set"/>.</summary>
    public static bool Intersects(IReadOnlyList<Vector3Int> cells, ISet<Vector3Int> set)
    {
        for (int i = 0; i < cells.Count; i++)
            if (set.Contains(cells[i])) return true;
        return false;
    }

    /// <summary>
    /// Clears <paramref name="results"/> and fills it with every cell within <paramref name="range"/> steps of
    /// at least one of <paramref name="cells"/>: the union of the circles around each cell. Same breadth-first
    /// expansion as <see cref="CircleShape"/>, so for a single cell the result equals that circle.
    /// </summary>
    public static void CollectCellsInRange(IReadOnlyList<Vector3Int> cells, int range, HashSet<Vector3Int> results)
    {
        results.Clear();
        var frontier = new List<Vector3Int>(cells.Count);
        for (int i = 0; i < cells.Count; i++)
            if (results.Add(cells[i])) frontier.Add(cells[i]);

        for (int dist = 1; dist <= range && frontier.Count > 0; dist++)
        {
            var next = new List<Vector3Int>();
            foreach (var cell in frontier)
                foreach (var nb in PathFindingUtils.GetNeighbors(cell))
                    if (results.Add(nb)) next.Add(nb);
            frontier = next;
        }
    }

    private static Vector3Int CubeToCell(Vector3Int cube, int z)
    {
        Vector3Int cell = HexCoordinates.CubeToOffset(cube);
        cell.z = z;
        return cell;
    }
}
