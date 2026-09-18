using NUnit.Framework;
using UnityEngine;

namespace SillyPirates.Tests.EditMode
{
    /// <summary>
    /// EditMode cannot pump frames, so an Awaitable can only be observed here if it already ran to
    /// completion — which is the case for any async Awaitable method that never awaits a frame.
    /// </summary>
    internal static class AwaitableTestUtils
    {
        /// <summary>
        /// Asserts the Awaitable completed synchronously and then consumes its result, so a faulted
        /// Awaitable rethrows here instead of disappearing as an unobserved exception.
        /// </summary>
        internal static void RunSynchronously(Awaitable awaitable)
        {
            Assert.That(awaitable, Is.Not.Null);
            Assert.That(awaitable.IsCompleted, Is.True,
                "The Awaitable did not complete synchronously: this code path needs a PlayMode test.");

            awaitable.GetAwaiter().GetResult();
        }
    }
}
