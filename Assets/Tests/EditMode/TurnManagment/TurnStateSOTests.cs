using System;
using NUnit.Framework;
using UnityEngine;

namespace SillyPirates.Tests.EditMode.TurnManagment
{
    /// <summary>
    /// The synchronous surface of TurnStateSO: who is active, whether it is the player's turn, and the
    /// reset that a new combat depends on. The Awaitable handshake is covered only where it completes
    /// without needing a frame.
    /// </summary>
    public class TurnStateSOTests : ScriptableObjectFixture
    {
        private TurnStateSO _turnState;

        [SetUp]
        public void SetUp() => _turnState = NewSO<TurnStateSO>();

        [Test]
        public void SetActiveCharacter_Always_StoresTheAgent()
        {
            FakeTurnAgent agent = new FakeTurnAgent();

            _turnState.SetActiveCharacter(agent);

            Assert.That(_turnState.ActiveAgent, Is.SameAs(agent));
        }

        [Test]
        public void SetActiveCharacter_PlayerTaggedAgent_SetsIsPlayerTurnTrue()
        {
            FakeTurnAgent agent = new FakeTurnAgent { Tag = "Player" };

            _turnState.SetActiveCharacter(agent);

            Assert.That(_turnState.IsPlayerTurn, Is.True);
        }

        [Test]
        public void SetActiveCharacter_NonPlayerAgent_SetsIsPlayerTurnFalse()
        {
            FakeTurnAgent agent = new FakeTurnAgent { Tag = "Enemy" };

            _turnState.SetActiveCharacter(agent);

            Assert.That(_turnState.IsPlayerTurn, Is.False);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void SetActiveCharacter_WithActionIndex_StoresTheActionIndex(int actionIndex)
        {
            _turnState.SetActiveCharacter(new FakeTurnAgent(), actionIndex);

            Assert.That(_turnState.CurrentActionIndex, Is.EqualTo(actionIndex));
        }

        [Test]
        public void NotifyAgentActivated_WithChannel_RaisesWithTheActiveAgent()
        {
            TurnAgentEventChannel channel = NewSO<TurnAgentEventChannel>();
            _turnState.OnAgentActivated = channel;
            FakeTurnAgent agent = new FakeTurnAgent();
            ITurnAgent received = null;
            channel.OnEventRaised += a => received = a;
            _turnState.SetActiveCharacter(agent);

            _turnState.NotifyAgentActivated();

            Assert.That(received, Is.SameAs(agent));
        }

        [Test]
        public void NotifyAgentActivated_WithNoChannel_DoesNotThrow()
        {
            _turnState.SetActiveCharacter(new FakeTurnAgent());

            Assert.That(() => _turnState.NotifyAgentActivated(), Throws.Nothing);
        }

        [Test]
        public void SignalTurnEnd_BeforeSetActiveCharacter_DoesNotThrow()
        {
            Assert.That(() => _turnState.SignalTurnEnd(), Throws.Nothing);
        }

        /// <summary>
        /// The turn handshake: TurnController blocks on WaitUntilTurnFinished until whoever is acting calls
        /// SignalTurnEnd. With the completion source already signalled the await resolves without a frame,
        /// so it is observable in EditMode.
        /// </summary>
        [Test]
        public void SignalTurnEnd_AfterSetActiveCharacter_CompletesTheTurnAwaitable()
        {
            _turnState.SetActiveCharacter(new FakeTurnAgent());

            _turnState.SignalTurnEnd();

            AwaitableTestUtils.RunSynchronously(_turnState.WaitUntilTurnFinished());
        }

        /// <summary>
        /// Awaiting with no active turn is a caller bug (waiting before SetActiveCharacter, or after a
        /// Clear()). It fails fast with a descriptive exception instead of a bare NullReference.
        /// Note: the guard lives in an async method, so the exception surfaces on the returned Awaitable.
        /// </summary>
        [Test]
        public void WaitUntilTurnFinished_WithNoActiveTurn_ThrowsInvalidOperationException()
        {
            Awaitable awaitable = _turnState.WaitUntilTurnFinished();

            Assert.That(() => awaitable.GetAwaiter().GetResult(), Throws.InstanceOf<InvalidOperationException>());
        }

        [Test]
        public void WaitUntilTurnFinished_AfterClear_ThrowsInvalidOperationException()
        {
            _turnState.SetActiveCharacter(new FakeTurnAgent());
            _turnState.Clear();

            Awaitable awaitable = _turnState.WaitUntilTurnFinished();

            Assert.That(() => awaitable.GetAwaiter().GetResult(), Throws.InstanceOf<InvalidOperationException>());
        }

        /// <summary>
        /// Clear must reset everything, not just the agent: a stale CurrentActionIndex used to survive into
        /// the next combat and was read back by the turn order UI.
        /// </summary>
        [Test]
        public void Clear_AfterSetActiveCharacter_ResetsAgentFlagAndActionIndex()
        {
            _turnState.SetActiveCharacter(new FakeTurnAgent { Tag = "Player" }, 2);

            _turnState.Clear();

            Assert.That(_turnState.ActiveAgent, Is.Null);
            Assert.That(_turnState.IsPlayerTurn, Is.False);
            Assert.That(_turnState.CurrentActionIndex, Is.EqualTo(0));
        }

        [Test]
        public void ResetForNewCombat_AfterSetActiveCharacter_ResetsAgentFlagAndActionIndex()
        {
            _turnState.SetActiveCharacter(new FakeTurnAgent { Tag = "Player" }, 2);

            _turnState.ResetForNewCombat();

            Assert.That(_turnState.ActiveAgent, Is.Null);
            Assert.That(_turnState.IsPlayerTurn, Is.False);
            Assert.That(_turnState.CurrentActionIndex, Is.EqualTo(0));
        }
    }
}
