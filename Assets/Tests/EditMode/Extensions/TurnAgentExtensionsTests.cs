using NUnit.Framework;

namespace SillyPirates.Tests.EditMode.Extensions
{
    /// <summary>
    /// The pure extension helpers every turn agent routes its lifecycle through. All four raises are
    /// null-conditional, so an agent with an unassigned channel degrades quietly instead of throwing in
    /// the middle of a turn.
    /// </summary>
    public class TurnAgentExtensionsTests : ScriptableObjectFixture
    {
        /// <summary>The whole point of HandleStartingTurn: AP go back to the configured per-turn maximum.</summary>
        [TestCase(1)]
        [TestCase(3)]
        [TestCase(5)]
        public void HandleStartingTurn_SpentActionPoints_ResetsToMaxActionPointsPerTurn(int maxActionPoints)
        {
            TurnAgentDataSO data = Track(TurnAgentDataBuilder.New().WithMaxActionPoints(maxActionPoints).Build());
            FakeTurnAgent agent = new FakeTurnAgent { AgentData = data, RemainingActionPoints = 0 };

            agent.HandleStartingTurn();

            Assert.That(agent.RemainingActionPoints, Is.EqualTo(maxActionPoints));
        }

        [Test]
        public void HandleStartingTurn_UnspentActionPoints_DoesNotAccumulateAboveTheMaximum()
        {
            TurnAgentDataSO data = Track(TurnAgentDataBuilder.New().WithMaxActionPoints(3).Build());
            FakeTurnAgent agent = new FakeTurnAgent { AgentData = data, RemainingActionPoints = 3 };

            agent.HandleStartingTurn();

            Assert.That(agent.RemainingActionPoints, Is.EqualTo(3));
        }

        [Test]
        public void HandleCombatJoin_WithChannel_RaisesWithTheAgent()
        {
            TurnAgentEventChannel channel = NewSO<TurnAgentEventChannel>();
            FakeTurnAgent agent = new FakeTurnAgent { OnAgentJoin = channel };
            ITurnAgent received = null;
            channel.OnEventRaised += a => received = a;

            agent.HandleCombatJoin();

            Assert.That(received, Is.SameAs(agent));
        }

        [Test]
        public void HandleCombatJoin_WithNullChannel_DoesNotThrow()
        {
            FakeTurnAgent agent = new FakeTurnAgent();

            Assert.That(() => agent.HandleCombatJoin(), Throws.Nothing);
        }

        [Test]
        public void HandleCombatLeave_WithChannel_RaisesWithTheAgent()
        {
            TurnAgentEventChannel channel = NewSO<TurnAgentEventChannel>();
            FakeTurnAgent agent = new FakeTurnAgent { OnAgentLeave = channel };
            ITurnAgent received = null;
            channel.OnEventRaised += a => received = a;

            agent.HandleCombatLeave();

            Assert.That(received, Is.SameAs(agent));
        }

        [Test]
        public void HandleCombatLeave_WithNullChannel_DoesNotThrow()
        {
            FakeTurnAgent agent = new FakeTurnAgent();

            Assert.That(() => agent.HandleCombatLeave(), Throws.Nothing);
        }

        [Test]
        public void EmitProximityCheck_WithChannel_RaisesThePayload()
        {
            InteractableProximityEventChannel channel = NewSO<InteractableProximityEventChannel>();
            FakeTurnAgent agent = new FakeTurnAgent { ProximityChannel = channel };
            int receivedRange = -99;
            channel.OnEventRaised += payload => receivedRange = payload.Range;

            agent.EmitProximityCheck(new ProximityPayload(null, 4));

            Assert.That(receivedRange, Is.EqualTo(4));
        }

        /// <summary>
        /// This raise used to be the only one without a null check, so an agent with an unwired proximity
        /// channel threw mid-turn while the other three shrugged it off.
        /// </summary>
        [Test]
        public void EmitProximityCheck_WithNullChannel_DoesNotThrow()
        {
            FakeTurnAgent agent = new FakeTurnAgent();

            Assert.That(() => agent.EmitProximityCheck(ProximityPayload.Empty), Throws.Nothing);
        }
    }
}
