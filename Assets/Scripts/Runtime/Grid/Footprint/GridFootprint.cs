using System;
using UnityEngine;

/// <summary>
/// How a <see cref="GridElement"/> decides which cells it covers. Per-instance placement data, edited in the
/// Inspector like the transform. There is no shape, count or direction of its own: with
/// <see cref="FootprintSource.Collider"/> the cells follow the colliders, so rotating the element rotates them.
/// </summary>
[Serializable]
public struct GridFootprint
{
    [Tooltip("Single: only the cell under the pivot. Collider: every cell at least half covered by the " +
             "element's active non-trigger colliders, projected onto the grid; the pivot cell is always included. " +
             "Movable elements must stay Single: movement is single-cell.")]
    [SerializeField] private FootprintSource _source;

    public GridFootprint(FootprintSource source)
    {
        _source = source;
    }

    /// <summary>The one-cell footprint every element had before footprints existed.</summary>
    public static GridFootprint Single => new GridFootprint(FootprintSource.Single);

    /// <summary>A footprint derived from the element's colliders.</summary>
    public static GridFootprint FromColliders => new GridFootprint(FootprintSource.Collider);

    public FootprintSource Source => _source;
}
