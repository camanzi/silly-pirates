using NUnit.Framework;

namespace SillyPirates.Tests.EditMode.Utils
{
    /// <summary>
    /// Smoke coverage for the EditMode test pipeline, on the purest logic in the project.
    /// Expected values are derived from MathUtils.K == 2, not from observed output.
    /// </summary>
    public class MathUtilsTests
    {
        [TestCase(0f, 50f, 0)]
        [TestCase(-10f, 50f, 0)]
        [TestCase(50f, 0f, 100)]
        [TestCase(50f, 50f, 50)]
        [TestCase(2f, 1f, 80)]
        public void CalculateHitChance_KnownInputs_ReturnsExpectedPercentage(float accuracy, float evasion, int expected)
        {
            int actual = MathUtils.CalculateHitChance(accuracy, evasion);

            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCase(0, 0f)]
        [TestCase(-3, 0f)]
        [TestCase(1, 2.75f)]
        [TestCase(2, 6.5f)]
        public void CalculateOvercapBonus_KnownExtraPoints_ReturnsExpectedBonus(int extraPoints, float expected)
        {
            float actual = MathUtils.CalculateOvercapBonus(extraPoints);

            Assert.That(actual, Is.EqualTo(expected).Within(0.0001f));
        }
    }
}
