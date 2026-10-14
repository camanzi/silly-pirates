using System;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SillyPirates.Tests.EditMode.Grid.Footprint
{
    /// <summary>
    /// Coverage sampling of one hex cell. The hexagons are written out by hand (a regular pointy-top hex and
    /// the 2.5 x 3 box-inscribed hex of the ship grid), so the expectations do not go through
    /// HexCellGeometry; one test closes the loop against a real Grid and its WorldToCell.
    /// </summary>
    public class HexCoverageTests
    {
        private const float Tolerance = 1e-5f;

        // Spec: samples are pulled 0.75 of the way from the centre towards each corner / edge midpoint.
        private const float Inset = 0.75f;

        private GameObject _gridObject;

        [TearDown]
        public void TearDown()
        {
            if (_gridObject != null)
                Object.DestroyImmediate(_gridObject);
        }

        /// <summary>Pointy-top hex of radius 1 around <paramref name="c"/>, corner i at 60*i - 30 degrees (CCW).</summary>
        private static Vector3[] RegularHex(Vector3 c)
        {
            Vector3[] corners = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                float a = Mathf.Deg2Rad * (60f * i - 30f);
                corners[i] = c + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
            }
            return corners;
        }

        /// <summary>The hex Unity draws inside a w x h cell box: points at +-h/2, sides at (+-w/2, +-h/4). CCW.</summary>
        private static Vector3[] BoxInscribedHex(Vector3 c, float w, float h)
        {
            return new[]
            {
                c + new Vector3(w / 2f, -h / 4f, 0f),
                c + new Vector3(w / 2f, h / 4f, 0f),
                c + new Vector3(0f, h / 2f, 0f),
                c + new Vector3(-w / 2f, h / 4f, 0f),
                c + new Vector3(-w / 2f, -h / 4f, 0f),
                c + new Vector3(0f, -h / 2f, 0f),
            };
        }

        private static Vector3[] Samples(Vector3 centre, Vector3[] corners)
        {
            Vector3[] samples = new Vector3[HexCoverage.SampleCount];
            HexCoverage.BuildSamples(centre, corners, samples);
            return samples;
        }

        /// <summary>
        /// Smallest signed distance from <paramref name="p"/> to the edges of a CCW convex polygon in the XY plane:
        /// positive strictly inside, zero on an edge, negative outside.
        /// </summary>
        private static float InsideMargin(Vector3[] polygon, Vector3 p)
        {
            float margin = float.MaxValue;
            for (int i = 0; i < polygon.Length; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % polygon.Length];
                Vector2 edge = (b - a).normalized;
                Vector2 toP = (Vector2)p - a;
                margin = Mathf.Min(margin, edge.x * toP.y - edge.y * toP.x);
            }
            return margin;
        }

        /// <summary>Thirteen distinct points along X: (0,0,0) .. (12,0,0), plus <paramref name="extra"/> beyond them.</summary>
        private static Vector3[] PointsAlongX(int extra = 0)
        {
            Vector3[] points = new Vector3[HexCoverage.SampleCount + extra];
            for (int i = 0; i < points.Length; i++) points[i] = new Vector3(i, 0f, 0f);
            return points;
        }

        // ---------------------------------------------------------------- constants

        [Test]
        public void SampleCount_IsCentrePlusOnePerCornerPlusOnePerEdge()
        {
            Assert.That(HexCoverage.SampleCount, Is.EqualTo(1 + 6 + 6));
        }

        [Test]
        public void CoverageThreshold_IsOneTenth()
        {
            Assert.That(HexCoverage.CoverageThreshold, Is.EqualTo(0.1f));
        }

        // ---------------------------------------------------------------- BuildSamples

        [Test]
        public void BuildSamples_AnyCell_FirstSampleIsTheCentre()
        {
            Vector3 centre = new Vector3(3f, -2f, 0f);

            Vector3[] samples = Samples(centre, RegularHex(centre));

            Assert.That(Vector3.Distance(samples[0], centre), Is.LessThan(Tolerance));
        }

        [Test]
        public void BuildSamples_OddSlots_AreInsetTowardsCornerI()
        {
            Vector3 centre = new Vector3(3f, -2f, 0f);
            Vector3[] corners = RegularHex(centre);

            Vector3[] samples = Samples(centre, corners);

            for (int i = 0; i < 6; i++)
            {
                Vector3 expected = centre + (corners[i] - centre) * Inset;
                Assert.That(Vector3.Distance(samples[1 + i * 2], expected), Is.LessThan(Tolerance), $"corner {i}");
            }
        }

        [Test]
        public void BuildSamples_EvenSlots_AreInsetTowardsTheMidpointOfEdgeIToIPlusOne()
        {
            Vector3 centre = new Vector3(3f, -2f, 0f);
            Vector3[] corners = RegularHex(centre);

            Vector3[] samples = Samples(centre, corners);

            for (int i = 0; i < 6; i++)
            {
                Vector3 edgeMid = (corners[i] + corners[(i + 1) % 6]) / 2f;
                Vector3 expected = centre + (edgeMid - centre) * Inset;
                Assert.That(Vector3.Distance(samples[2 + i * 2], expected), Is.LessThan(Tolerance), $"edge {i}");
            }
        }

        [Test]
        public void BuildSamples_RegularHex_AllSamplesStrictlyInside()
        {
            Vector3 centre = new Vector3(-1f, 4f, 0f);
            Vector3[] corners = RegularHex(centre);

            Vector3[] samples = Samples(centre, corners);

            // A sample on a shared border would count for both neighbours: it must keep clear of every edge.
            for (int i = 0; i < samples.Length; i++)
                Assert.That(InsideMargin(corners, samples[i]), Is.GreaterThan(0.1f), $"sample {i}");
        }

        [Test]
        public void BuildSamples_ShipCellSize_AllSamplesStrictlyInside()
        {
            Vector3 centre = new Vector3(10f, 0f, 0f);
            Vector3[] corners = BoxInscribedHex(centre, 2.5f, 3f);

            Vector3[] samples = Samples(centre, corners);

            for (int i = 0; i < samples.Length; i++)
                Assert.That(InsideMargin(corners, samples[i]), Is.GreaterThan(0.1f), $"sample {i}");
        }

        [Test]
        public void BuildSamples_AnyCell_AllSamplesAreDistinct()
        {
            Vector3 centre = Vector3.zero;

            Vector3[] samples = Samples(centre, RegularHex(centre));

            for (int i = 0; i < samples.Length; i++)
                for (int j = i + 1; j < samples.Length; j++)
                    Assert.That(Vector3.Distance(samples[i], samples[j]), Is.GreaterThan(0.1f), $"samples {i}/{j}");
        }

        /// <summary>
        /// Closes the loop with the real grid of the ship (Hexagon, 2.5 x 3, YZX): every sample built from
        /// HexCellGeometry's corners falls back into its own cell, for cells of both row parities.
        /// </summary>
        [TestCase(2, 2)]
        [TestCase(3, 3)]
        [TestCase(0, -1)]
        [TestCase(-2, -4)]
        public void BuildSamples_RealShipGrid_EverySampleMapsBackToItsOwnCell(int x, int y)
        {
            _gridObject = new GameObject("HexCoverageTests Grid");
            _gridObject.transform.SetPositionAndRotation(new Vector3(4f, 1f, -3f), Quaternion.Euler(0f, 20f, 0f));
            UnityEngine.Grid grid = _gridObject.AddComponent<UnityEngine.Grid>();
            grid.cellLayout = GridLayout.CellLayout.Hexagon;
            grid.cellSize = new Vector3(2.5f, 3f, 1f);
            grid.cellSwizzle = GridLayout.CellSwizzle.YZX;
            Vector3Int cell = new Vector3Int(x, y, 0);
            Vector3[] corners = new Vector3[6];
            HexCellGeometry.GetCornersWorld(grid, cell, corners);

            Vector3[] samples = Samples(HexCellGeometry.CellCenterWorld(grid, cell), corners);

            for (int i = 0; i < samples.Length; i++)
                Assert.That(grid.WorldToCell(samples[i]), Is.EqualTo(cell), $"sample {i} at {samples[i]}");
        }

        [Test]
        public void BuildSamples_FiveCorners_ThrowsArgumentException()
        {
            Assert.That(() => HexCoverage.BuildSamples(Vector3.zero, new Vector3[5], new Vector3[HexCoverage.SampleCount]),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void BuildSamples_NullCorners_ThrowsArgumentException()
        {
            Assert.That(() => HexCoverage.BuildSamples(Vector3.zero, null, new Vector3[HexCoverage.SampleCount]),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void BuildSamples_SamplesArrayTooShort_ThrowsArgumentException()
        {
            Assert.That(() => HexCoverage.BuildSamples(Vector3.zero, RegularHex(Vector3.zero), new Vector3[HexCoverage.SampleCount - 1]),
                Throws.TypeOf<ArgumentException>());
        }

        // ---------------------------------------------------------------- Fraction

        [Test]
        public void Fraction_EverySampleCovered_ReturnsOne()
        {
            float fraction = HexCoverage.Fraction(PointsAlongX(), _ => true);

            Assert.That(fraction, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void Fraction_NoSampleCovered_ReturnsZero()
        {
            float fraction = HexCoverage.Fraction(PointsAlongX(), _ => false);

            Assert.That(fraction, Is.EqualTo(0f).Within(Tolerance));
        }

        [TestCase(1)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(12)]
        public void Fraction_FirstKSamplesCovered_ReturnsKOverThirteen(int k)
        {
            float fraction = HexCoverage.Fraction(PointsAlongX(), p => p.x < k);

            Assert.That(fraction, Is.EqualTo(k / 13f).Within(Tolerance));
        }

        /// <summary>With 13 samples there is no exact tenth: 1 covered falls below the threshold, 2 reach it.</summary>
        [TestCase(1, false)]
        [TestCase(2, true)]
        public void Fraction_AroundTheThreshold_OneOfThirteenIsOutTwoIsIn(int covered, bool expectedIncluded)
        {
            float fraction = HexCoverage.Fraction(PointsAlongX(), p => p.x < covered);

            Assert.That(fraction >= HexCoverage.CoverageThreshold, Is.EqualTo(expectedIncluded));
        }

        [Test]
        public void Fraction_ArrayLongerThanSampleCount_IgnoresTheExtraEntries()
        {
            // Only entries 13..19 are covered: they are past SampleCount and must not be read.
            float fraction = HexCoverage.Fraction(PointsAlongX(extra: 7), p => p.x >= HexCoverage.SampleCount);

            Assert.That(fraction, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Fraction_ArrayTooShort_ThrowsArgumentException()
        {
            Assert.That(() => HexCoverage.Fraction(new Vector3[HexCoverage.SampleCount - 1], _ => true),
                Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Fraction_NullPredicate_ThrowsArgumentNullException()
        {
            Assert.That(() => HexCoverage.Fraction(PointsAlongX(), null),
                Throws.TypeOf<ArgumentNullException>());
        }
    }
}
