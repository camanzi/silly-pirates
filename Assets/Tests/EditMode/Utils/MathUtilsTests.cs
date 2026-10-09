using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;

namespace SillyPirates.Tests.EditMode.Utils
{
    /// <summary>
    /// MathUtils lost CalculateHitChance (attacks always hit) and CalculateOvercapBonus (no more overcap):
    /// only EvaluateBezierPoint is left.
    ///
    /// Expected values come from the quadratic Bezier definition
    /// B(t) = (1-t)^2 * p0 + 2(1-t)t * p1 + t^2 * p2, worked out by hand per case.
    /// </summary>
    public class MathUtilsTests
    {
        private const float Tolerance = 1e-5f;
        private static readonly Vector3EqualityComparer Approx = new Vector3EqualityComparer(Tolerance);

        private static readonly Vector3 P0 = new Vector3(0f, 0f, 0f);
        private static readonly Vector3 P1 = new Vector3(2f, 4f, 0f);
        private static readonly Vector3 P2 = new Vector3(4f, 0f, 0f);

        [Test]
        public void EvaluateBezierPoint_TZero_ReturnsStartPoint()
        {
            Vector3 result = MathUtils.EvaluateBezierPoint(0f, P0, P1, P2);

            Assert.That(result, Is.EqualTo(P0).Using(Approx));
        }

        [Test]
        public void EvaluateBezierPoint_TOne_ReturnsEndPoint()
        {
            Vector3 result = MathUtils.EvaluateBezierPoint(1f, P0, P1, P2);

            Assert.That(result, Is.EqualTo(P2).Using(Approx));
        }

        /// <summary>Weights 0.25 / 0.5 / 0.25: x = 0.5*2 + 0.25*4 = 2, y = 0.5*4 = 2.</summary>
        [Test]
        public void EvaluateBezierPoint_THalf_WeightsControlPointByOneHalf()
        {
            Vector3 result = MathUtils.EvaluateBezierPoint(0.5f, P0, P1, P2);

            Assert.That(result, Is.EqualTo(new Vector3(2f, 2f, 0f)).Using(Approx));
        }

        /// <summary>Weights 0.5625 / 0.375 / 0.0625: x = 0.375*2 + 0.0625*4 = 1, y = 0.375*4 = 1.5.</summary>
        [Test]
        public void EvaluateBezierPoint_TQuarter_IsNotALinearInterpolation()
        {
            Vector3 result = MathUtils.EvaluateBezierPoint(0.25f, P0, P1, P2);

            Assert.That(result, Is.EqualTo(new Vector3(1f, 1.5f, 0f)).Using(Approx));
        }

        /// <summary>
        /// With the control point at the midpoint of a segment the curve degenerates into that segment,
        /// traversed at constant speed: B(t) = p0 + t * (p2 - p0).
        /// </summary>
        [TestCase(0f)]
        [TestCase(0.1f)]
        [TestCase(0.5f)]
        [TestCase(0.9f)]
        [TestCase(1f)]
        public void EvaluateBezierPoint_CollinearMidpointControl_FollowsTheStraightSegment(float t)
        {
            Vector3 start = new Vector3(1f, -2f, 3f);
            Vector3 end = new Vector3(5f, 6f, -1f);
            Vector3 mid = (start + end) * 0.5f;

            Vector3 result = MathUtils.EvaluateBezierPoint(t, start, mid, end);

            Assert.That(result, Is.EqualTo(start + t * (end - start)).Using(Approx));
        }
    }
}
