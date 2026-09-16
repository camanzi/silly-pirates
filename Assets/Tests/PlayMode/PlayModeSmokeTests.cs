using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SillyPirates.Tests.PlayMode
{
    /// <summary>
    /// Proves the PlayMode assembly compiles and runs with a live player loop.
    /// Frame-based waiting is the one thing EditMode tests cannot do.
    /// </summary>
    public class PlayModeSmokeTests
    {
        [UnityTest]
        public IEnumerator YieldingOneFrame_AdvancesTheFrameCount()
        {
            int before = Time.frameCount;

            yield return null;

            Assert.That(Time.frameCount, Is.GreaterThan(before));
        }
    }
}
