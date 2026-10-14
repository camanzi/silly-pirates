using UnityEngine;

/// <summary>
/// Conversions between the grid's offset coordinates and cube coordinates.
/// The grid uses pointy-top hexes with odd rows shifted right ("odd-r"), matching the neighbour tables in
/// <see cref="PathFindingUtils"/>. Cube coordinates are stored as <c>(q, r, s)</c> with <c>q + r + s == 0</c>.
/// Offset Z (the layer) is ignored by the conversion: callers that need it must carry it over themselves.
/// </summary>
public static class HexCoordinates
{
    // Indexed by HexDirection, counter-clockwise starting from East: rotating a direction by one step is
    // the same as moving one slot forward in this table.
    private static readonly Vector3Int[] CubeDirections =
    {
        new Vector3Int( 1,  0, -1), // East
        new Vector3Int( 0,  1, -1), // NorthEast
        new Vector3Int(-1,  1,  0), // NorthWest
        new Vector3Int(-1,  0,  1), // West
        new Vector3Int( 0, -1,  1), // SouthWest
        new Vector3Int( 1, -1,  0), // SouthEast
    };

    public const int DirectionCount = 6;

    public static Vector3Int OffsetToCube(Vector3Int hex)
    {
        // y - (y & 1) is always even, so the division is exact for negative rows too.
        int q = hex.x - (hex.y - (hex.y & 1)) / 2;
        int r = hex.y;
        return new Vector3Int(q, r, -q - r);
    }

    /// <summary>Inverse of <see cref="OffsetToCube"/>. The returned Z is always 0.</summary>
    public static Vector3Int CubeToOffset(Vector3Int cube)
    {
        int r = cube.y;
        int x = cube.x + (r - (r & 1)) / 2;
        return new Vector3Int(x, r, 0);
    }

    /// <summary>The unit cube vector one step in <paramref name="direction"/>. Out-of-range values wrap.</summary>
    public static Vector3Int DirectionToCube(HexDirection direction)
        => CubeDirections[Mod((int)direction, DirectionCount)];

    /// <summary>
    /// Rotates a cube vector around the origin by 60 degrees per step: positive steps turn counter-clockwise
    /// (East toward NorthEast), negative steps clockwise.
    /// </summary>
    public static Vector3Int RotateCube(Vector3Int cube, int steps)
    {
        int n = Mod(steps, DirectionCount);
        for (int i = 0; i < n; i++)
            cube = new Vector3Int(-cube.y, -cube.z, -cube.x);
        return cube;
    }

    /// <summary>The offset cell one step from <paramref name="cell"/> in <paramref name="direction"/>, keeping its Z.</summary>
    public static Vector3Int Neighbour(Vector3Int cell, HexDirection direction)
    {
        Vector3Int result = CubeToOffset(OffsetToCube(cell) + DirectionToCube(direction));
        result.z = cell.z;
        return result;
    }

    /// <summary>Hex distance in cells between two cube coordinates.</summary>
    public static int CubeDistance(Vector3Int a, Vector3Int b)
        => (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z)) / 2;

    private static int Mod(int value, int modulus)
    {
        int m = value % modulus;
        return m < 0 ? m + modulus : m;
    }
}
