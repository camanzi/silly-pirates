using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure footprint geometry: moves a footprint between absolute offset cells and anchor-relative cube offsets,
/// and answers distance/overlap questions about a set of cells. No Unity objects involved, so it is
/// EditMode-testable.
/// </summary>
public static class HexFootprint
{
    /// <summary>
    /// Clears <paramref name="results"/> and fills it with the cube-coordinate offsets of
    /// <paramref name="cells"/> from <paramref name="anchor"/>. Guarantees: <c>results[0]</c> is the zero offset
    /// (the anchor) whether or not <paramref name="cells"/> contains it, and no duplicates. Offset Z (the layer)
    /// is dropped: <see cref="Translate"/> puts every cell back on the new anchor's layer.
    /// </summary>
    public static void ToOffsets(Vector3Int anchor, IReadOnlyList<Vector3Int> cells, List<Vector3Int> results)
    {
        results.Clear();
        results.Add(Vector3Int.zero);
        if (cells == null) return;

        Vector3Int anchorCube = HexCoordinates.OffsetToCube(anchor);
        for (int i = 0; i < cells.Count; i++)
        {
            Vector3Int offset = HexCoordinates.OffsetToCube(cells[i]) - anchorCube;
            if (!results.Contains(offset)) results.Add(offset);
        }
    }

    /// <summary>
    /// Clears <paramref name="results"/> and fills it with the cells of a footprint placed on
    /// <paramref name="anchor"/>, from the cube offsets produced by <see cref="ToOffsets"/>. Guarantees:
    /// <c>results[0] == anchor</c> even with null or empty offsets, the order of the offsets is kept, zero
    /// offsets are skipped, and every cell keeps <c>anchor.z</c>. Translating in cube space (not offset space)
    /// keeps the shape intact when the anchor moves to a row of the other parity.
    /// </summary>
    public static void Translate(Vector3Int anchor, IReadOnlyList<Vector3Int> cubeOffsets, List<Vector3Int> results)
    {
        results.Clear();
        results.Add(anchor);
        if (cubeOffsets == null) return;

        Vector3Int anchorCube = HexCoordinates.OffsetToCube(anchor);
        for (int i = 0; i < cubeOffsets.Count; i++)
        {
            if (cubeOffsets[i] == Vector3Int.zero) continue;
            results.Add(CubeToCell(anchorCube + cubeOffsets[i], anchor.z));
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
