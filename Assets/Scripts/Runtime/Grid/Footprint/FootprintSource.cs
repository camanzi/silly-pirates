/// <summary>
/// Where a <see cref="GridElement"/>'s footprint comes from. Values are explicit and serialized: never
/// renumber them, append new entries instead.
/// </summary>
public enum FootprintSource
{
    /// <summary>Only the anchor cell, the one under the pivot. The default, so existing prefabs keep their behaviour.</summary>
    Single = 0,
    /// <summary>
    /// The cells covered by the element's active, non-trigger colliders projected onto the grid (see
    /// <see cref="ColliderFootprintProbe"/>). The anchor is always included. Rotating the element rotates the footprint.
    /// </summary>
    Collider = 1,
}
