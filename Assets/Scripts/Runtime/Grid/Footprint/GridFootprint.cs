using System;
using UnityEngine;

/// <summary>
/// Which cells a <see cref="GridElement"/> covers, relative to its anchor cell (the cell under its pivot,
/// i.e. <see cref="GridElement.gridPosition"/>). Per-instance placement data, edited in the Inspector like
/// the transform. Resolve it into concrete cells with <see cref="HexFootprint.Resolve(Vector3Int, GridFootprint, System.Collections.Generic.List{Vector3Int})"/>.
/// </summary>
[Serializable]
public struct GridFootprint
{
    [Tooltip("Single: one cell. Line: a straight row centred on the pivot. Cluster: the pivot cell plus its " +
             "neighbours. The collider is NOT derived from this: size it by hand to match the mesh.")]
    [SerializeField] private FootprintShape _shape;

    [Tooltip("Number of cells. Clamped to 1 for Single, 1..9 for Line, 1..7 for Cluster.")]
    [SerializeField] private int _count;

    [Tooltip("Grid-space direction. Line: the axis (with an even count the extra cell goes this way). " +
             "Cluster: the first neighbour, the others follow counter-clockwise.")]
    [SerializeField] private HexDirection _direction;

    public GridFootprint(FootprintShape shape, int count, HexDirection direction)
    {
        _shape = shape;
        _count = count;
        _direction = direction;
    }

    /// <summary>The one-cell footprint every element had before footprints existed.</summary>
    public static GridFootprint Single => new GridFootprint(FootprintShape.Single, 1, HexDirection.East);

    public FootprintShape Shape => _shape;
    public HexDirection Direction => _direction;

    /// <summary>The effective number of cells, already clamped for the shape.</summary>
    public int Count => HexFootprint.ClampCount(_shape, _count);

    /// <summary>
    /// True when the direction changes which cells are covered: false for Single and for any one-cell
    /// footprint, where rotating is a no-op.
    /// </summary>
    public bool HasDirection => _shape != FootprintShape.Single && Count > 1;

    /// <summary>A copy with the count clamped to the shape's legal range (used by OnValidate).</summary>
    public GridFootprint Clamped() => new GridFootprint(_shape, Count, _direction);

    /// <summary>A copy pointing toward <paramref name="direction"/>. Out-of-range values wrap.</summary>
    public GridFootprint WithDirection(HexDirection direction)
        => new GridFootprint(_shape, Count, WrapDirection((int)direction));

    /// <summary>
    /// A copy rotated by 60 degrees per step: positive steps turn counter-clockwise (East toward NorthEast),
    /// negative steps clockwise. Any number of steps is accepted and wraps around.
    /// </summary>
    public GridFootprint Rotated(int steps)
        => new GridFootprint(_shape, Count, WrapDirection((int)_direction + steps));

    /// <summary>A copy with <paramref name="count"/> cells, clamped to the shape's legal range.</summary>
    public GridFootprint WithCount(int count)
        => new GridFootprint(_shape, HexFootprint.ClampCount(_shape, count), _direction);

    /// <summary>A copy with another shape. The current count is kept where legal, clamped otherwise.</summary>
    public GridFootprint WithShape(FootprintShape shape)
        => new GridFootprint(shape, HexFootprint.ClampCount(shape, Count), _direction);

    private static HexDirection WrapDirection(int value)
    {
        int m = value % HexCoordinates.DirectionCount;
        return (HexDirection)(m < 0 ? m + HexCoordinates.DirectionCount : m);
    }
}
