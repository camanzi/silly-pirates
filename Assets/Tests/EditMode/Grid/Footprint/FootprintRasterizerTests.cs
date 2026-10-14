using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SillyPirates.Tests.EditMode.Grid.Footprint
{
    /// <summary>
    /// Footprint rasterisation against coverage functions written inline as lambdas: no collider, no grid.
    /// The last section feeds it a rectangle sampled through HexCoverage on the idealised planar grid, i.e.
    /// the whole pure pipeline the collider probe runs, minus the physics.
    /// </summary>
    public class FootprintRasterizerTests
    {
        private const float Threshold = 0.5f;

        private static Vector3Int Cell(int x, int y, int z = 0) => new Vector3Int(x, y, z);

        private static Func<Vector3Int, float> Covering(params Vector3Int[] cells)
        {
            HashSet<Vector3Int> set = new HashSet<Vector3Int>(cells);
            return c => set.Contains(c) ? 1f : 0f;
        }

        private static List<Vector3Int> Rasterize(Vector3Int anchor, Vector3Int min, Vector3Int max,
                                                  Func<Vector3Int, float> coverage, int maxCells = 64)
        {
            List<Vector3Int> results = new List<Vector3Int>();
            FootprintRasterizer.Rasterize(anchor, min, max, coverage, Threshold, maxCells, results);
            return results;
        }

        // ---------------------------------------------------------------- the anchor

        [Test]
        public void Rasterize_ZeroCoverageEverywhere_ReturnsTheAnchorAlone()
        {
            List<Vector3Int> results = Rasterize(Cell(2, 2), Cell(0, 0), Cell(4, 4), _ => 0f);

            Assert.That(results, Is.EqualTo(new[] { Cell(2, 2) }));
        }

        [Test]
        public void Rasterize_AnchorInTheMiddleOfACoveredRange_IsFirst()
        {
            List<Vector3Int> results = Rasterize(Cell(2, 2), Cell(0, 0), Cell(4, 4), _ => 1f);

            Assert.That(results[0], Is.EqualTo(Cell(2, 2)));
        }

        [Test]
        public void Rasterize_AnyCoverage_NeverAsksForTheAnchor()
        {
            List<Vector3Int> asked = new List<Vector3Int>();

            Rasterize(Cell(2, 2), Cell(0, 0), Cell(4, 4), c => { asked.Add(c); return 1f; });

            Assert.That(asked, Has.No.Member(Cell(2, 2)));
        }

        [Test]
        public void Rasterize_AnchorOutsideTheRange_IsStillFirstAndIncluded()
        {
            List<Vector3Int> results = Rasterize(Cell(-5, -5), Cell(0, 0), Cell(1, 0), _ => 1f);

            Assert.That(results, Is.EqualTo(new[] { Cell(-5, -5), Cell(0, 0), Cell(1, 0) }));
        }

        // ---------------------------------------------------------------- the other cells

        [Test]
        public void Rasterize_FullCoverage_ReturnsEveryCellInTheRangeExactlyOnce()
        {
            List<Vector3Int> results = Rasterize(Cell(1, 1), Cell(0, 0), Cell(2, 2), _ => 1f);

            Assert.That(results, Is.Unique);
            Assert.That(results, Has.Count.EqualTo(9));
        }

        [Test]
        public void Rasterize_FullCoverage_OtherCellsFollowInRowMajorOrder()
        {
            List<Vector3Int> results = Rasterize(Cell(1, 1), Cell(0, 0), Cell(2, 1), _ => 1f);

            Assert.That(results, Is.EqualTo(new[]
            {
                Cell(1, 1),
                Cell(0, 0), Cell(1, 0), Cell(2, 0),
                Cell(0, 1), Cell(2, 1),
            }));
        }

        [Test]
        public void Rasterize_RangeIsInclusive_CornersOfTheRangeAreCandidates()
        {
            List<Vector3Int> results = Rasterize(Cell(0, 0), Cell(-2, -3), Cell(4, 5),
                Covering(Cell(-2, -3), Cell(4, 5), Cell(-2, 5), Cell(4, -3)));

            Assert.That(results, Is.EquivalentTo(new[] { Cell(0, 0), Cell(-2, -3), Cell(4, -3), Cell(-2, 5), Cell(4, 5) }));
        }

        [Test]
        public void Rasterize_CoveredCellsOutsideTheRange_AreNotIncluded()
        {
            List<Vector3Int> results = Rasterize(Cell(0, 0), Cell(-1, -1), Cell(1, 1), Covering(Cell(2, 0), Cell(0, -2)));

            Assert.That(results, Is.EqualTo(new[] { Cell(0, 0) }));
        }

        [Test]
        public void Rasterize_DisjointShapes_KeepsBothAndTheAnchor()
        {
            List<Vector3Int> results = Rasterize(Cell(0, 0), Cell(-6, -6), Cell(6, 6),
                Covering(Cell(5, 5), Cell(-5, -4), Cell(-4, -4)));

            Assert.That(results, Is.EqualTo(new[] { Cell(0, 0), Cell(-5, -4), Cell(-4, -4), Cell(5, 5) }));
        }

        [TestCase(0.5f, true)]
        [TestCase(0.49999f, false)]
        [TestCase(0.50001f, true)]
        [TestCase(1f, true)]
        [TestCase(0f, false)]
        public void Rasterize_CoverageAroundTheThreshold_IncludesFromTheThresholdUp(float coverage, bool included)
        {
            List<Vector3Int> results = Rasterize(Cell(0, 0), Cell(0, 0), Cell(1, 0), c => c == Cell(1, 0) ? coverage : 0f);

            Assert.That(results.Contains(Cell(1, 0)), Is.EqualTo(included));
        }

        /// <summary>The project threshold sits between 1/13 and 2/13 sampled points: pin it end to end.</summary>
        [TestCase(1, false)]
        [TestCase(2, true)]
        public void Rasterize_ProjectThresholdWithThirteenSamples_TwoCoveredIsTheMinimum(int hits, bool included)
        {
            List<Vector3Int> results = new List<Vector3Int>();

            FootprintRasterizer.Rasterize(Cell(0, 0), Cell(0, 0), Cell(1, 0),
                c => c == Cell(1, 0) ? hits / (float)HexCoverage.SampleCount : 0f,
                HexCoverage.CoverageThreshold, 64, results);

            Assert.That(results.Contains(Cell(1, 0)), Is.EqualTo(included));
        }

        // ---------------------------------------------------------------- the layer

        [Test]
        public void Rasterize_AnchorOnALayer_EveryCellKeepsTheAnchorLayer()
        {
            List<Vector3Int> results = Rasterize(Cell(1, 1, 3), Cell(0, 0, -7), Cell(2, 2, 9), _ => 1f);

            Assert.That(results.ConvertAll(c => c.z), Has.All.EqualTo(3));
        }

        [Test]
        public void Rasterize_AnchorOnALayer_CoverageIsAskedOnTheAnchorLayer()
        {
            List<int> askedLayers = new List<int>();

            Rasterize(Cell(1, 1, 3), Cell(0, 0, 0), Cell(2, 2, 0), c => { askedLayers.Add(c.z); return 0f; });

            Assert.That(askedLayers, Has.All.EqualTo(3));
        }

        /// <summary>The anchor is skipped by value including z: an anchor on another layer is still recognised.</summary>
        [Test]
        public void Rasterize_AnchorOnALayerInsideTheRange_IsNotDuplicated()
        {
            List<Vector3Int> results = Rasterize(Cell(1, 1, 3), Cell(0, 0, 0), Cell(2, 2, 0), _ => 1f);

            Assert.That(results, Is.Unique);
            Assert.That(results, Has.Count.EqualTo(9));
        }

        // ---------------------------------------------------------------- cap and results list

        [Test]
        public void Rasterize_MoreCoveredCellsThanTheCap_ReturnsFalse()
        {
            List<Vector3Int> results = new List<Vector3Int>();

            bool complete = FootprintRasterizer.Rasterize(Cell(0, 0), Cell(-2, -2), Cell(2, 2), _ => 1f, Threshold, 4, results);

            Assert.That(complete, Is.False);
        }

        [Test]
        public void Rasterize_MoreCoveredCellsThanTheCap_StopsAtTheCapWithTheAnchorFirst()
        {
            List<Vector3Int> results = Rasterize(Cell(0, 0), Cell(-2, -2), Cell(2, 2), _ => 1f, maxCells: 4);

            Assert.That(results, Is.EqualTo(new[] { Cell(0, 0), Cell(-2, -2), Cell(-1, -2), Cell(0, -2) }));
        }

        [Test]
        public void Rasterize_CoveredCellsExactlyAtTheCap_ReturnsTrueWithAllOfThem()
        {
            List<Vector3Int> results = new List<Vector3Int>();

            bool complete = FootprintRasterizer.Rasterize(Cell(0, 0), Cell(-2, -2), Cell(2, 2),
                Covering(Cell(1, 0), Cell(-1, 0), Cell(2, 2)), Threshold, 4, results);

            Assert.That(complete, Is.True);
            Assert.That(results, Has.Count.EqualTo(4));
        }

        [Test]
        public void Rasterize_UncoveredCellsBeyondTheCap_DoNotCountTowardsIt()
        {
            List<Vector3Int> results = new List<Vector3Int>();

            bool complete = FootprintRasterizer.Rasterize(Cell(0, 0), Cell(-5, -5), Cell(5, 5),
                Covering(Cell(5, 5)), Threshold, 2, results);

            Assert.That(complete, Is.True);
            Assert.That(results, Is.EqualTo(new[] { Cell(0, 0), Cell(5, 5) }));
        }

        [TestCase(0)]
        [TestCase(-3)]
        public void Rasterize_CapBelowOne_StillReturnsTheAnchor(int maxCells)
        {
            List<Vector3Int> results = new List<Vector3Int>();

            bool complete = FootprintRasterizer.Rasterize(Cell(0, 0), Cell(-1, -1), Cell(1, 1), _ => 1f, Threshold, maxCells, results);

            Assert.That(results, Is.EqualTo(new[] { Cell(0, 0) }));
            Assert.That(complete, Is.False);
        }

        [Test]
        public void Rasterize_NonEmptyResults_ClearsThemFirst()
        {
            List<Vector3Int> results = new List<Vector3Int> { Cell(9, 9), Cell(8, 8), Cell(7, 7) };

            FootprintRasterizer.Rasterize(Cell(0, 0), Cell(0, 0), Cell(0, 0), _ => 1f, Threshold, 64, results);

            Assert.That(results, Is.EqualTo(new[] { Cell(0, 0) }));
        }

        [Test]
        public void Rasterize_EmptyRange_ReturnsTheAnchorAndTrue()
        {
            List<Vector3Int> results = new List<Vector3Int>();

            bool complete = FootprintRasterizer.Rasterize(Cell(0, 0), Cell(3, 3), Cell(2, 2), _ => 1f, Threshold, 64, results);

            Assert.That(complete, Is.True);
            Assert.That(results, Is.EqualTo(new[] { Cell(0, 0) }));
        }

        [Test]
        public void Rasterize_NullResults_ThrowsArgumentNullException()
        {
            Assert.That(() => FootprintRasterizer.Rasterize(Cell(0, 0), Cell(0, 0), Cell(1, 1), _ => 1f, Threshold, 64, null),
                Throws.TypeOf<ArgumentNullException>());
        }

        [Test]
        public void Rasterize_NullCoverage_ThrowsArgumentNullException()
        {
            Assert.That(() => FootprintRasterizer.Rasterize(Cell(0, 0), Cell(0, 0), Cell(1, 1), null, Threshold, 64, new List<Vector3Int>()),
                Throws.TypeOf<ArgumentNullException>());
        }

        // ---------------------------------------------------------------- a sampled rectangle (pure pipeline)

        /// <summary>
        /// Coverage of a rectangle on the idealised planar grid (hex radius 1, odd-r), sampled with HexCoverage.
        /// The rectangle is centred on the centre of <paramref name="anchor"/>, 3 cells long (3 * sqrt(3)) and 1
        /// wide, its long axis at <paramref name="angleDegrees"/> from East (CCW). Along East this covers 7 of
        /// the 13 samples of each end cell (centre, the 4 side corners' samples at |y| = 0.375 and the two
        /// samples towards the E/W edge midpoints) and none of any other cell, so the expected footprint is the
        /// anchor and its two neighbours along the axis. Hex symmetry makes that hold at every multiple of 60.
        /// </summary>
        private static Func<Vector3Int, float> SampledRectangle(Vector3Int anchor, float angleDegrees)
        {
            Vector2 centre = HexCellGeometry.CellCenterPlanar(anchor, 1f);
            Quaternion inverse = Quaternion.Euler(0f, 0f, -angleDegrees);
            float halfLength = 1.5f * Mathf.Sqrt(3f);
            const float halfWidth = 0.5f;

            Vector2[] corners2 = new Vector2[6];
            Vector3[] corners = new Vector3[6];
            Vector3[] samples = new Vector3[HexCoverage.SampleCount];

            return cell =>
            {
                Vector2 cellCentre = HexCellGeometry.CellCenterPlanar(cell, 1f);
                HexCellGeometry.GetCornersPlanar(cellCentre, 1f, corners2);
                for (int i = 0; i < 6; i++) corners[i] = corners2[i];
                HexCoverage.BuildSamples(cellCentre, corners, samples);

                return HexCoverage.Fraction(samples, p =>
                {
                    Vector3 local = inverse * (p - (Vector3)centre);
                    return Mathf.Abs(local.x) <= halfLength && Mathf.Abs(local.y) <= halfWidth;
                });
            };
        }

        private static List<Vector3Int> RasterizeRectangle(Vector3Int anchor, float angleDegrees)
        {
            List<Vector3Int> results = new List<Vector3Int>();
            FootprintRasterizer.Rasterize(anchor, anchor - new Vector3Int(4, 4, 0), anchor + new Vector3Int(4, 4, 0),
                SampledRectangle(anchor, angleDegrees), HexCoverage.CoverageThreshold, 64, results);
            return results;
        }

        [TestCase(2, 2)]
        [TestCase(2, 3)]
        public void Rasterize_RectangleAlongEast_CoversTheAnchorAndItsEastAndWestNeighbours(int x, int y)
        {
            Vector3Int anchor = Cell(x, y);

            List<Vector3Int> results = RasterizeRectangle(anchor, 0f);

            Assert.That(results[0], Is.EqualTo(anchor));
            Assert.That(results, Is.EquivalentTo(new[]
            {
                anchor,
                HexCoordinates.Neighbour(anchor, HexDirection.East),
                HexCoordinates.Neighbour(anchor, HexDirection.West),
            }));
        }

        [TestCase(2, 2)]
        [TestCase(2, 3)]
        public void Rasterize_RectangleRotated60_CoversTheAnchorAndItsNorthEastAndSouthWestNeighbours(int x, int y)
        {
            Vector3Int anchor = Cell(x, y);

            List<Vector3Int> results = RasterizeRectangle(anchor, 60f);

            Assert.That(results[0], Is.EqualTo(anchor));
            Assert.That(results, Is.EquivalentTo(new[]
            {
                anchor,
                HexCoordinates.Neighbour(anchor, HexDirection.NorthEast),
                HexCoordinates.Neighbour(anchor, HexDirection.SouthWest),
            }));
        }

        /// <summary>
        /// At 30 degrees the rectangle points at a corner and grazes every cell it crosses with 3 of 13 samples,
        /// above the threshold: the anchor, its E/NE/W/SW neighbours, and the two cells two steps away along the
        /// 30 degree diagonal (East then NorthEast, West then SouthWest).
        /// </summary>
        [Test]
        public void Rasterize_RectangleRotated30_CoversEveryCellItGrazes()
        {
            Vector3Int anchor = Cell(2, 2);
            Vector3Int east = HexCoordinates.Neighbour(anchor, HexDirection.East);
            Vector3Int west = HexCoordinates.Neighbour(anchor, HexDirection.West);

            List<Vector3Int> results = RasterizeRectangle(anchor, 30f);

            Assert.That(results[0], Is.EqualTo(anchor));
            Assert.That(results, Is.EquivalentTo(new[]
            {
                anchor,
                east,
                west,
                HexCoordinates.Neighbour(anchor, HexDirection.NorthEast),
                HexCoordinates.Neighbour(anchor, HexDirection.SouthWest),
                HexCoordinates.Neighbour(east, HexDirection.NorthEast),
                HexCoordinates.Neighbour(west, HexDirection.SouthWest),
            }));
        }
    }
}
