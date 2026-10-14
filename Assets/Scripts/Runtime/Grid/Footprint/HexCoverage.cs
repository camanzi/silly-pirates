using System;
using UnityEngine;

/// <summary>
/// Pure coverage sampling of one hex cell: a fixed pattern of points inside the cell, and the fraction of them
/// a caller-supplied test reports as covered. No physics involved (the test comes in as a delegate, like
/// <see cref="PathFindingUtils"/>), so it is EditMode-testable with a lambda shape.
/// </summary>
public static class HexCoverage
{
    /// <summary>Centre, plus one point towards each of the 6 corners, plus one towards each of the 6 edge midpoints.</summary>
    public const int SampleCount = 1 + HexCellGeometry.CornerCount * 2;

    /// <summary>
    /// A cell is part of a collider footprint when at least this fraction of its samples is covered. With 13
    /// samples one hit (1/13) stays below it, so in practice a cell needs 2 covered samples (~15%).
    /// </summary>
    public const float CoverageThreshold = 0.1f;

    // Samples are pulled towards the centre so that a point never sits exactly on a border shared with a
    // neighbouring cell, where the same hit would count for both.
    private const float SampleInset = 0.75f;

    /// <summary>
    /// Fills <paramref name="samples"/> (at least <see cref="SampleCount"/> slots) with the sample points of a
    /// cell: index 0 is <paramref name="centre"/>, then, for each corner i, a point towards corner i and one
    /// towards the midpoint of edge (i, i+1). Works in any space (world or planar) as long as the corners do,
    /// e.g. the output of <see cref="HexCellGeometry.GetCornersWorld"/>.
    /// </summary>
    public static void BuildSamples(Vector3 centre, Vector3[] corners, Vector3[] samples)
    {
        if (corners == null || corners.Length < HexCellGeometry.CornerCount)
            throw new ArgumentException($"Needs {HexCellGeometry.CornerCount} corners.", nameof(corners));
        if (samples == null || samples.Length < SampleCount)
            throw new ArgumentException($"Needs room for {SampleCount} samples.", nameof(samples));

        samples[0] = centre;
        for (int i = 0; i < HexCellGeometry.CornerCount; i++)
        {
            Vector3 corner = corners[i];
            Vector3 edgeMid = (corner + corners[(i + 1) % HexCellGeometry.CornerCount]) * 0.5f;
            samples[1 + i * 2] = Vector3.LerpUnclamped(centre, corner, SampleInset);
            samples[2 + i * 2] = Vector3.LerpUnclamped(centre, edgeMid, SampleInset);
        }
    }

    /// <summary>
    /// The fraction (0..1) of the first <see cref="SampleCount"/> entries of <paramref name="samples"/> for
    /// which <paramref name="isCovered"/> returns true.
    /// </summary>
    public static float Fraction(Vector3[] samples, Func<Vector3, bool> isCovered)
    {
        if (samples == null || samples.Length < SampleCount)
            throw new ArgumentException($"Needs {SampleCount} samples.", nameof(samples));
        if (isCovered == null) throw new ArgumentNullException(nameof(isCovered));

        int hits = 0;
        for (int i = 0; i < SampleCount; i++)
            if (isCovered(samples[i])) hits++;
        return hits / (float)SampleCount;
    }
}
