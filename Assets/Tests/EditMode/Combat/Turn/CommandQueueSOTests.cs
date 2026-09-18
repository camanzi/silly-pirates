using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace SillyPirates.Tests.EditMode.Combat.Turn
{
    /// <summary>
    /// The command queue: FIFO draining, the re-entrancy guard, and the finally block that keeps a
    /// throwing command from wedging the queue for the rest of the combat.
    ///
    /// Every command used here completes synchronously, so the whole ProcessQueueAsync runs to completion
    /// inside the call and EditMode never needs a frame.
    /// </summary>
    public class CommandQueueSOTests : ScriptableObjectFixture
    {
        private CommandQueueSO _queue;
        private List<string> _log;

        [SetUp]
        public void SetUp()
        {
            _queue = NewSO<CommandQueueSO>();
            _log = new List<string>();
        }

        [Test]
        public void ProcessQueueAsync_EmptyQueue_CompletesWithoutError()
        {
            AwaitableTestUtils.RunSynchronously(_queue.ProcessQueueAsync());

            Assert.That(_log, Is.Empty);
        }

        [Test]
        public void ProcessQueueAsync_MultipleCommands_ExecutesInFifoOrder()
        {
            _queue.AddCommand(new FakeCommand("a", _log));
            _queue.AddCommand(new FakeCommand("b", _log));
            _queue.AddCommand(new FakeCommand("c", _log));

            AwaitableTestUtils.RunSynchronously(_queue.ProcessQueueAsync());

            Assert.That(_log, Is.EqualTo(new[] { "a", "b", "c" }));
        }

        [Test]
        public void ProcessQueueAsync_MultipleCommands_ExecutesEachExactlyOnce()
        {
            FakeCommand first = new FakeCommand("a", _log);
            FakeCommand second = new FakeCommand("b", _log);
            _queue.AddCommand(first);
            _queue.AddCommand(second);

            AwaitableTestUtils.RunSynchronously(_queue.ProcessQueueAsync());

            Assert.That(first.ExecuteCount, Is.EqualTo(1));
            Assert.That(second.ExecuteCount, Is.EqualTo(1));
        }

        /// <summary>
        /// The _isProcessing guard, exercised from inside the drain itself: the first command calls
        /// ProcessQueueAsync again while the outer call is still running.
        ///
        /// The assertion is on the interleaving, not just on the execution counts: without the guard the
        /// re-entrant call would drain "b" in the middle of "a", giving a-start / b / a-end. With the
        /// guard it bails out immediately and "b" only runs once "a" is done.
        /// </summary>
        [Test]
        public void ProcessQueueAsync_ReentrantCallWhileProcessing_DoesNotDrainQueueInsideRunningCommand()
        {
            FakeCommand second = new FakeCommand("b", _log);
            FakeCommand first = new FakeCommand("a-start", _log, onExecute: () =>
            {
                AwaitableTestUtils.RunSynchronously(_queue.ProcessQueueAsync());
                _log.Add("a-end");
            });
            _queue.AddCommand(first);
            _queue.AddCommand(second);

            AwaitableTestUtils.RunSynchronously(_queue.ProcessQueueAsync());

            Assert.That(_log, Is.EqualTo(new[] { "a-start", "a-end", "b" }));
            Assert.That(first.ExecuteCount, Is.EqualTo(1));
            Assert.That(second.ExecuteCount, Is.EqualTo(1));
        }

        /// <summary>
        /// The finally block the source calls mandatory: without it a throwing command would leave
        /// _isProcessing true forever and this SO, shared across every turn, would silently swallow every
        /// command from then on.
        /// </summary>
        [Test]
        public void ProcessQueueAsync_CommandThrows_StillDrainsLaterCommands()
        {
            _queue.AddCommand(new FakeCommand("boom", _log, throwOnExecute: new InvalidOperationException("boom")));
            Assert.That(() => AwaitableTestUtils.RunSynchronously(_queue.ProcessQueueAsync()),
                Throws.InstanceOf<InvalidOperationException>());

            _queue.AddCommand(new FakeCommand("after", _log));
            AwaitableTestUtils.RunSynchronously(_queue.ProcessQueueAsync());

            Assert.That(_log, Is.EqualTo(new[] { "boom", "after" }));
        }

        [Test]
        public void Clear_WithPendingCommands_DiscardsThem()
        {
            _queue.AddCommand(new FakeCommand("a", _log));
            _queue.AddCommand(new FakeCommand("b", _log));

            _queue.Clear();
            AwaitableTestUtils.RunSynchronously(_queue.ProcessQueueAsync());

            Assert.That(_log, Is.Empty);
        }

        [Test]
        public void ResetForNewCombat_WithPendingCommands_DiscardsThem()
        {
            _queue.AddCommand(new FakeCommand("a", _log));

            _queue.ResetForNewCombat();
            AwaitableTestUtils.RunSynchronously(_queue.ProcessQueueAsync());

            Assert.That(_log, Is.Empty);
        }

        [Test]
        public void ProcessQueueAsync_CalledTwice_DoesNotReexecuteDrainedCommands()
        {
            FakeCommand command = new FakeCommand("a", _log);
            _queue.AddCommand(command);

            AwaitableTestUtils.RunSynchronously(_queue.ProcessQueueAsync());
            AwaitableTestUtils.RunSynchronously(_queue.ProcessQueueAsync());

            Assert.That(command.ExecuteCount, Is.EqualTo(1));
        }
    }
}
