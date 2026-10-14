using System;
using UnityEngine;

/// <summary>
/// Hex cell geometry, with no drawing API involved: the editor overlay, the Inspector mini-map and a future
/// runtime renderer all build on it.
/// <para>The <c>World</c> methods read the real <see cref="GridLayout"/>, so any cell size and swizzle work.
/// The <c>Planar</c> methods describe an idealised pointy-top grid (East = +x, North = +y, odd rows shifted
/// right as in <see cref="HexCoordinates"/>), for views that have no scene to read, such as a UI preview.</para>
/// </summary>
public static class HexCellGeometry
{
    public const int CornerCount = 6;

    private static readonly float Sqrt3 = Mathf.Sqrt(3f);

    // Scratch buffer for PlaneNormalWorld. Main-thread only, like the rest of the grid code.
    private static readonly Vector3[] NormalCorners = new Vector3[CornerCount];

    /// <summary>
    /// World-space centre of <paramref name="cell"/>. <see cref="GridLayout"/> has no GetCellCenterWorld (only
    /// <see cref="Grid"/> and Tilemap do): this is the same computation.
    /// </summary>
    public static Vector3 CellCenterWorld(GridLayout layout, Vector3Int cell)
        => layout.LocalToWorld(layout.CellToLocalInterpolated((Vector3)cell + layout.GetLayoutCellCenter()));

    /// <summary>
    /// Fills <paramref name="corners"/> (at least 6 slots) with the world-space corners of
    /// <paramref name="cell"/>, in neighbour order: corner i sits between neighbour i-1 and neighbour i.
    /// Each corner of a regular hex is at (d_i + d_{i+1}) / 3 from the centre, where d_i is the vector to the
    /// i-th neighbour's centre: this holds whatever the cell size and swizzle are.
    /// </summary>
    public static void GetCornersWorld(GridLayout layout, Vector3Int cell, Vector3[] corners)
    {
        if (layout == null) throw new ArgumentNullException(nameof(layout));
        if (corners == null || corners.Length < CornerCount)
            throw new ArgumentException($"Needs room for {CornerCount} corners.", nameof(corners));

        Vector3 centre = CellCenterWorld(layout, cell);
        Vector3 prevNeighbour =
            CellCenterWorld(layout, HexCoordinates.Neighbour(cell, (HexDirection)(CornerCount - 1))) - centre;

        for (int i = 0; i < CornerCount; i++)
        {
            Vector3 neighbour = CellCenterWorld(layout, HexCoordinates.Neighbour(cell, (HexDirection)i)) - centre;
            corners[i] = centre + (prevNeighbour + neighbour) / 3f;
            prevNeighbour = neighbour;
        }
    }

    /// <summary>
    /// Unit normal of the grid plane at <paramref name="cell"/>, pointing to where the elements stand: the
    /// direction of increasing cell Z, or world up when the layout has no cell height. "Counter-clockwise" in
    /// the editor tools means counter-clockwise when looking down this normal.
    /// </summary>
    public static Vector3 PlaneNormalWorld(GridLayout layout, Vector3Int cell)
    {
        if (layout == null) throw new ArgumentNullException(nameof(layout));

        GetCornersWorld(layout, cell, NormalCorners);
        Vector3 normal = Vector3.Cross(NormalCorners[1] - NormalCorners[0], NormalCorners[2] - NormalCorners[0]);
        normal = normal.sqrMagnitude > 0f ? normal.normalized : Vector3.up;

        Vector3 up = CellCenterWorld(layout, cell + Vector3Int.forward) - CellCenterWorld(layout, cell);
        if (up.sqrMagnitude < 1e-6f) up = Vector3.up;
        return Vector3.Dot(normal, up) < 0f ? -normal : normal;
    }

    /// <summary>
    /// Centre of <paramref name="cell"/> on the idealised planar grid, relative to the centre of cell (0,0).
    /// <paramref name="size"/> is the hex radius (centre to corner). +y is North.
    /// </summary>
    public static Vector2 CellCenterPlanar(Vector3Int cell, float size)
    {
        Vector3Int cube = HexCoordinates.OffsetToCube(cell);
        return new Vector2(size * Sqrt3 * (cube.x + cube.y * 0.5f), size * 1.5f * cube.y);
    }

    /// <summary>
    /// Fills <paramref name="corners"/> (at least 6 slots) with the corners of a pointy-top hex of radius
    /// <paramref name="size"/> centred on <paramref name="centre"/>, in the same neighbour order as
    /// <see cref="GetCornersWorld"/>. +y is North.
    /// </summary>
    public static void GetCornersPlanar(Vector2 centre, float size, Vector2[] corners)
    {
        if (corners == null || corners.Length < CornerCount)
            throw new ArgumentException($"Needs room for {CornerCount} corners.", nameof(corners));

        // Corner i sits between neighbour i-1 and neighbour i: East is at 0 degrees, so corner 0 is at -30.
        for (int i = 0; i < CornerCount; i++)
        {
            float angle = Mathf.Deg2Rad * (60f * i - 30f);
            corners[i] = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * size;
        }
    }
}
