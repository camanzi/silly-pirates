using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The physics adapter of the collider footprint: projects an element's colliders (see
/// <see cref="ElementColliders"/>) onto the grid and feeds the coverage to the pure
/// <see cref="FootprintRasterizer"/> / <see cref="HexCoverage"/> core.
/// <para>Each sample is a <see cref="Collider.Raycast"/> along the grid plane normal, from above the colliders'
/// bounds to below them. That works for every collider type (non-convex meshes included), ignores layers and
/// does not go through a scene query, so it behaves the same in a scene and in Prefab Mode; a disabled
/// collider never hits, which is why only the currently active ones count.</para>
/// <para>Allocation-free (static scratch buffers and cached delegates): main thread only, like the rest of the
/// grid code. Called once per registration and by the editor preview, never per frame.</para>
/// </summary>
public static class ColliderFootprintProbe
{
    /// <summary>Upper bound on the cells of one footprint. Past it the footprint is truncated with a warning.</summary>
    public const int MaxCells = 64;

    // Extra length on both ends of the sample rays, so a face lying exactly on the bounds still gets hit.
    private const float RayMargin = 0.1f;

    private static readonly List<Collider> Colliders = new List<Collider>(8);
    private static readonly Vector3[] Corners = new Vector3[HexCellGeometry.CornerCount];
    private static readonly Vector3[] Samples = new Vector3[HexCoverage.SampleCount];

    private static readonly Func<Vector3Int, float> CellCoverage = ComputeCellCoverage;
    private static readonly Func<Vector3, bool> PointCovered = IsPointCovered;

    // State of the probe in progress, read by the cached delegates above.
    private static GridLayout _layout;
    private static Vector3 _normal;
    private static Bounds _bounds;
    private static float _rayLength;

    /// <summary>
    /// Clears <paramref name="results"/> and fills it with the cells covered by the colliders of
    /// <paramref name="root"/>, with the guarantees of <see cref="FootprintRasterizer.Rasterize"/>: index 0 is
    /// <paramref name="anchor"/>, which is always included.
    /// </summary>
    /// <returns>False when the element has no active non-trigger collider: <paramref name="results"/> then
    /// holds the anchor alone.</returns>
    public static bool Probe(Transform root, GridLayout layout, Vector3Int anchor, List<Vector3Int> results)
    {
        if (root == null) throw new ArgumentNullException(nameof(root));
        if (layout == null) throw new ArgumentNullException(nameof(layout));
        if (results == null) throw new ArgumentNullException(nameof(results));

        // Auto Sync Transforms is off in this project: without this, colliders moved or rotated since the last
        // simulation step (always the case in the editor, and right after spawning) are sampled where they were.
        Physics.SyncTransforms();

        ElementColliders.Collect(root, Colliders);
        if (!ElementColliders.TryGetBounds(Colliders, out Bounds bounds))
        {
            results.Clear();
            results.Add(anchor);
            return false;
        }

        _layout = layout;
        _bounds = bounds;
        _normal = HexCellGeometry.PlaneNormalWorld(layout, anchor);
        _rayLength = bounds.extents.magnitude * 2f + RayMargin * 2f;

        GetCandidateRange(layout, bounds, out Vector3Int min, out Vector3Int max);
        bool complete = FootprintRasterizer.Rasterize(anchor, min, max, CellCoverage,
                                                      HexCoverage.CoverageThreshold, MaxCells, results);
        if (!complete)
        {
            Debug.LogWarning(
                $"[{nameof(ColliderFootprintProbe)}] '{root.name}': the colliders cover more than {MaxCells} cells, " +
                "the footprint was truncated. Check that no large collider (e.g. a deck or hull) sits under the element.",
                root);
        }

        _layout = null;
        Colliders.Clear();
        return true;
    }

    /// <summary>The offset cells under the eight corners of <paramref name="bounds"/>, padded by one cell.</summary>
    private static void GetCandidateRange(GridLayout layout, Bounds bounds, out Vector3Int min, out Vector3Int max)
    {
        min = new Vector3Int(int.MaxValue, int.MaxValue, 0);
        max = new Vector3Int(int.MinValue, int.MinValue, 0);
        Vector3 c = bounds.center;
        Vector3 e = bounds.extents;

        for (int i = 0; i < 8; i++)
        {
            var corner = new Vector3(
                c.x + ((i & 1) == 0 ? -e.x : e.x),
                c.y + ((i & 2) == 0 ? -e.y : e.y),
                c.z + ((i & 4) == 0 ? -e.z : e.z));
            Vector3Int cell = layout.WorldToCell(corner);
            min.x = Mathf.Min(min.x, cell.x);
            min.y = Mathf.Min(min.y, cell.y);
            max.x = Mathf.Max(max.x, cell.x);
            max.y = Mathf.Max(max.y, cell.y);
        }

        min.x -= 1; min.y -= 1;
        max.x += 1; max.y += 1;
    }

    private static float ComputeCellCoverage(Vector3Int cell)
    {
        HexCellGeometry.GetCornersWorld(_layout, cell, Corners);
        HexCoverage.BuildSamples(HexCellGeometry.CellCenterWorld(_layout, cell), Corners, Samples);
        return HexCoverage.Fraction(Samples, PointCovered);
    }

    private static bool IsPointCovered(Vector3 point)
    {
        // Start above the top of the bounds (measured along the normal) and travel down through all of it.
        float above = Vector3.Dot(_bounds.center - point, _normal) + _bounds.extents.magnitude + RayMargin;
        var ray = new Ray(point + _normal * above, -_normal);

        for (int i = 0; i < Colliders.Count; i++)
            if (Colliders[i].Raycast(ray, out _, _rayLength)) return true;
        return false;
    }
}
