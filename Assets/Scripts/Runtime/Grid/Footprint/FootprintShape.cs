/// <summary>
/// The shape of the cells a <see cref="GridElement"/> covers. Values are explicit and serialized: never
/// renumber them, append new entries instead.
/// </summary>
public enum FootprintShape
{
    /// <summary>One cell: the anchor itself. The default, so existing prefabs keep their behaviour.</summary>
    Single = 0,
    /// <summary>A straight row of cells along the footprint direction, centred on the anchor.</summary>
    Line = 1,
    /// <summary>The anchor plus its neighbours, taken counter-clockwise starting from the footprint direction.</summary>
    Cluster = 2,
    // Custom = 3 is reserved for hand-authored footprints and not implemented yet.
}
