using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure rasterisation of a footprint: walks a rectangle of candidate offset cells and keeps those whose
/// coverage reaches a threshold. The coverage comes in as a delegate (in the game it is
/// <see cref="ColliderFootprintProbe"/> sampling the colliders), so any shape can be tested with a lambda.
/// </summary>
public static class FootprintRasterizer
{
    /// <summary>
    /// Clears <paramref name="results"/> and fills it with the footprint cells. Guarantees:
    /// <c>results[0] == anchor</c> whatever its coverage, no duplicates, every cell keeps <c>anchor.z</c>, at
    /// most <paramref name="maxCells"/> cells (never fewer than 1). The other cells follow in row-major order
    /// (y, then x) and need not be connected to the anchor.
    /// </summary>
    /// <param name="min">Inclusive lower corner of the candidate range (offset x/y; z is ignored).</param>
    /// <param name="max">Inclusive upper corner of the candidate range (offset x/y; z is ignored).</param>
    /// <param name="coverage">Fraction (0..1) of a cell covered by the element. Never called for the anchor.</param>
    /// <returns>False when cells above the threshold were dropped because of <paramref name="maxCells"/>.</returns>
    public static bool Rasterize(Vector3Int anchor, Vector3Int min, Vector3Int max,
                                 Func<Vector3Int, float> coverage, float threshold, int maxCells,
                                 List<Vector3Int> results)
    {
        if (results == null) throw new ArgumentNullException(nameof(results));
        if (coverage == null) throw new ArgumentNullException(nameof(coverage));

        results.Clear();
        results.Add(anchor);
        int cap = Mathf.Max(1, maxCells);

        for (int y = min.y; y <= max.y; y++)
        {
            for (int x = min.x; x <= max.x; x++)
            {
                var cell = new Vector3Int(x, y, anchor.z);
                if (cell == anchor) continue;
                if (coverage(cell) < threshold) continue;
                if (results.Count >= cap) return false;
                results.Add(cell);
            }
        }
        return true;
    }
}
