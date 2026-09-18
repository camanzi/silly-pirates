using NUnit.Framework;
using UnityEngine;

namespace SillyPirates.Tests.EditMode.TurnManagment
{
    /// <summary>
    /// The turn queue: ordering, Action Value arithmetic and the guards that silently do nothing.
    ///
    /// Every expected AV comes from the source formula CalculateBaseAV = 10000 / Mathf.Max(1, agility)
    /// and from the documented sort (ascending CurrentAV, ties broken by descending EffectiveAgility).
    /// Agilities are chosen so the AVs are exact: 200 -> 50, 100 -> 100, 80 -> 125, 50 -> 200.
    /// </summary>
    public class TurnOrderDataSOTests : ScriptableObjectFixture
    {
        private const float Tolerance = 0.001f;

        private TurnOrderDataSO _turnOrder;
        private VoidEventChannel _queueUpdated;
        private int _queueUpdatedCount;

        [SetUp]
        public void SetUp()
        {
            Random.InitState(1234); // only the randomized-initial-AV test depends on this

            _turnOrder = NewSO<TurnOrderDataSO>();
            _queueUpdated = NewSO<VoidEventChannel>();
            _turnOrder.OnQueueUpdated = _queueUpdated;

            _queueUpdatedCount = 0;
            _queueUpdated.OnEventRaised += CountQueueUpdate;
        }

        [TearDown]
        public void UnsubscribeChannel() => _queueUpdated.OnEventRaised -= CountQueueUpdate;

        private void CountQueueUpdate() => _queueUpdatedCount++;

        private static FakeTurnAgent Agent(int agility, string name = "agent") =>
            new FakeTurnAgent { EffectiveAgility = agility, DisplayName = name };

        // ---------------------------------------------------------------- AddEntity

        [TestCase(100, 100f)]
        [TestCase(200, 50f)]
        [TestCase(50, 200f)]
        [TestCase(1, 10000f)]
        [TestCase(0, 10000f)]  // Mathf.Max(1, agility) clamp
        [TestCase(-5, 10000f)] // same clamp, negative side
        public void AddEntity_KnownAgility_SetsInitialAVFromBaseFormula(int agility, float expectedAV)
        {
            FakeTurnAgent agent = Agent(agility);

            _turnOrder.AddEntity(agent);

            Assert.That(_turnOrder.TurnQueue[0].CurrentAV, Is.EqualTo(expectedAV).Within(Tolerance));
        }

        [Test]
        public void AddEntity_SameAgentTwice_QueueContainsItOnce()
        {
            FakeTurnAgent agent = Agent(100);

            _turnOrder.AddEntity(agent);
            _turnOrder.AddEntity(agent);

            Assert.That(_turnOrder.TurnQueue, Has.Count.EqualTo(1));
        }

        [Test]
        public void AddEntity_AgentsWithDifferentAgility_SortsQueueByAscendingAV()
        {
            FakeTurnAgent slow = Agent(50, "slow");    // AV 200
            FakeTurnAgent fast = Agent(200, "fast");   // AV 50
            FakeTurnAgent middle = Agent(100, "mid");  // AV 100

            _turnOrder.AddEntity(slow);
            _turnOrder.AddEntity(fast);
            _turnOrder.AddEntity(middle);

            Assert.That(_turnOrder.TurnQueue[0].Agent, Is.SameAs(fast));
            Assert.That(_turnOrder.TurnQueue[1].Agent, Is.SameAs(middle));
            Assert.That(_turnOrder.TurnQueue[2].Agent, Is.SameAs(slow));
        }

        /// <summary>The randomize branch is guarded by an AgentData null check.</summary>
        [Test]
        public void AddEntity_NullAgentData_DoesNotRandomiseInitialAV()
        {
            FakeTurnAgent agent = Agent(100);
            Assert.That(agent.AgentData, Is.Null, "Precondition: the fake carries no agent data.");

            _turnOrder.AddEntity(agent);

            Assert.That(_turnOrder.TurnQueue[0].CurrentAV, Is.EqualTo(100f).Within(Tolerance));
        }

        /// <summary>
        /// With RandomizeInitialAV on, the first AV varies by at most InitialAVVarianceRatio (0.2) of the
        /// base value. Twenty agents are added so the test also fails if the randomisation were dropped and
        /// every AV came back exactly at the base.
        /// </summary>
        [Test]
        public void AddEntity_RandomizeInitialAVEnabled_KeepsAVWithinVarianceBand()
        {
            TurnAgentDataSO data = Track(TurnAgentDataBuilder.New().WithRandomizeInitialAV(true).Build());
            const float baseAV = 100f; // agility 100
            float variance = baseAV * TurnOrderDataSO.InitialAVVarianceRatio;

            for (int i = 0; i < 20; i++)
                _turnOrder.AddEntity(new FakeTurnAgent { EffectiveAgility = 100, AgentData = data });

            bool anyDiffersFromBase = false;
            foreach (EntityTurnState state in _turnOrder.TurnQueue)
            {
                Assert.That(state.CurrentAV, Is.InRange(baseAV - variance, baseAV + variance));
                Assert.That(state.CurrentAV, Is.GreaterThanOrEqualTo(1f));
                if (!Mathf.Approximately(state.CurrentAV, baseAV))
                    anyDiffersFromBase = true;
            }

            Assert.That(anyDiffersFromBase, Is.True, "No AV was randomised at all.");
        }

        [Test]
        public void AddEntity_NewAgent_RaisesQueueUpdatedEvent()
        {
            _turnOrder.AddEntity(Agent(100));

            Assert.That(_queueUpdatedCount, Is.EqualTo(1));
        }

        // ---------------------------------------------------------------- RemoveEntity / Clear

        [Test]
        public void RemoveEntity_AgentInQueue_RemovesItAndRaisesEvent()
        {
            FakeTurnAgent kept = Agent(100, "kept");
            FakeTurnAgent removed = Agent(50, "removed");
            _turnOrder.AddEntity(kept);
            _turnOrder.AddEntity(removed);
            _queueUpdatedCount = 0;

            _turnOrder.RemoveEntity(removed);

            Assert.That(_turnOrder.TurnQueue, Has.Count.EqualTo(1));
            Assert.That(_turnOrder.TurnQueue[0].Agent, Is.SameAs(kept));
            Assert.That(_queueUpdatedCount, Is.EqualTo(1));
        }

        /// <summary>The "index != -1" guard: no removal, and no event for listeners to react to.</summary>
        [Test]
        public void RemoveEntity_AgentNotInQueue_DoesNotRaiseQueueUpdatedEvent()
        {
            _turnOrder.AddEntity(Agent(100));
            _queueUpdatedCount = 0;

            _turnOrder.RemoveEntity(Agent(100, "stranger"));

            Assert.That(_turnOrder.TurnQueue, Has.Count.EqualTo(1));
            Assert.That(_queueUpdatedCount, Is.EqualTo(0));
        }

        [Test]
        public void Clear_PopulatedQueue_EmptiesQueue()
        {
            _turnOrder.AddEntity(Agent(100));
            _turnOrder.AddEntity(Agent(50));

            _turnOrder.Clear();

            Assert.That(_turnOrder.TurnQueue, Is.Empty);
        }

        [Test]
        public void ResetForNewCombat_PopulatedQueue_EmptiesQueue()
        {
            _turnOrder.AddEntity(Agent(100));

            _turnOrder.ResetForNewCombat();

            Assert.That(_turnOrder.TurnQueue, Is.Empty);
        }

        // ---------------------------------------------------------------- StartActiveTurn

        /// <summary>
        /// Starting a turn zeroes the head's AV and charges the elapsed time to everybody else:
        /// AVs 50 / 100 / 200 with 50 elapsed become 0 / 50 / 150.
        /// </summary>
        [Test]
        public void StartActiveTurn_PopulatedQueue_ZeroesHeadAndSubtractsElapsedFromOthers()
        {
            _turnOrder.AddEntity(Agent(200, "fast"));   // AV 50
            _turnOrder.AddEntity(Agent(100, "mid"));    // AV 100
            _turnOrder.AddEntity(Agent(50, "slow"));    // AV 200

            _turnOrder.StartActiveTurn();

            Assert.That(_turnOrder.TurnQueue[0].CurrentAV, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(_turnOrder.TurnQueue[1].CurrentAV, Is.EqualTo(50f).Within(Tolerance));
            Assert.That(_turnOrder.TurnQueue[2].CurrentAV, Is.EqualTo(150f).Within(Tolerance));
        }

        /// <summary>
        /// Mathf.Max(1f, ...) floor: an agent tied with the head would drop to 0 and become a second
        /// active agent, so it is clamped to 1.
        /// </summary>
        [Test]
        public void StartActiveTurn_FollowerWithSameAV_IsClampedToOne()
        {
            _turnOrder.AddEntity(Agent(100, "a"));
            _turnOrder.AddEntity(Agent(100, "b"));

            _turnOrder.StartActiveTurn();

            Assert.That(_turnOrder.TurnQueue[0].CurrentAV, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(_turnOrder.TurnQueue[1].CurrentAV, Is.EqualTo(1f).Within(Tolerance));
        }

        [Test]
        public void StartActiveTurn_EmptyQueue_DoesNotRaiseQueueUpdatedEvent()
        {
            _turnOrder.StartActiveTurn();

            Assert.That(_queueUpdatedCount, Is.EqualTo(0));
        }

        // ---------------------------------------------------------------- CompleteActiveTurn

        /// <summary>
        /// The "Count &lt; 2" early return. A one-agent queue is a legal state (last survivor), and the
        /// agent's AV is left exactly as it was — including the 0 that StartActiveTurn just wrote.
        /// </summary>
        [Test]
        public void CompleteActiveTurn_SingleAgentQueue_LeavesQueueAndAVUntouched()
        {
            FakeTurnAgent onlyAgent = Agent(100);
            _turnOrder.AddEntity(onlyAgent);
            _turnOrder.StartActiveTurn();
            _queueUpdatedCount = 0;

            _turnOrder.CompleteActiveTurn();

            Assert.That(_turnOrder.TurnQueue, Has.Count.EqualTo(1));
            Assert.That(_turnOrder.TurnQueue[0].Agent, Is.SameAs(onlyAgent));
            Assert.That(_turnOrder.TurnQueue[0].CurrentAV, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(_queueUpdatedCount, Is.EqualTo(0));
        }

        /// <summary>
        /// Agilities 100 (AV 100) and 90 (AV 1000/9 = 111.11). After StartActiveTurn the head is at 0 and
        /// the follower at 11.11; completing the turn gives the finished agent a fresh base AV of 100, so
        /// it lands behind the follower.
        /// </summary>
        [Test]
        public void CompleteActiveTurn_AfterStartActiveTurn_RotatesFinishedAgentBehindTheFollower()
        {
            FakeTurnAgent first = Agent(100, "first");
            FakeTurnAgent second = Agent(90, "second");
            _turnOrder.AddEntity(first);
            _turnOrder.AddEntity(second);
            _turnOrder.StartActiveTurn();

            _turnOrder.CompleteActiveTurn();

            Assert.That(_turnOrder.TurnQueue[0].Agent, Is.SameAs(second));
            Assert.That(_turnOrder.TurnQueue[1].Agent, Is.SameAs(first));
        }

        /// <summary>
        /// A plain ITurnAgent is not an IAbilityHolder, so ApplyAndConsumeAVDiscount returns early and the
        /// finished agent gets exactly the base AV, with no discount applied.
        /// </summary>
        [Test]
        public void CompleteActiveTurn_AgentWithoutPassiveController_AppliesNoAVDiscount()
        {
            FakeTurnAgent first = Agent(100, "first");
            _turnOrder.AddEntity(first);
            _turnOrder.AddEntity(Agent(90, "second"));
            _turnOrder.StartActiveTurn();

            _turnOrder.CompleteActiveTurn();

            Assert.That(_turnOrder.TurnQueue[1].CurrentAV, Is.EqualTo(100f).Within(Tolerance));
        }

        // ---------------------------------------------------------------- UpdateAgentAV / AdjustAgentAV

        [Test]
        public void UpdateAgentAV_AgilityChangedSinceJoin_RecomputesAVFromCurrentAgility()
        {
            FakeTurnAgent agent = Agent(100);
            _turnOrder.AddEntity(agent);
            agent.EffectiveAgility = 200;

            _turnOrder.UpdateAgentAV(agent);

            Assert.That(_turnOrder.TurnQueue[0].CurrentAV, Is.EqualTo(50f).Within(Tolerance));
        }

        [Test]
        public void UpdateAgentAV_AgentNotInQueue_DoesNotRaiseQueueUpdatedEvent()
        {
            _turnOrder.AddEntity(Agent(100));
            _queueUpdatedCount = 0;

            _turnOrder.UpdateAgentAV(Agent(100, "stranger"));

            Assert.That(_queueUpdatedCount, Is.EqualTo(0));
        }

        [Test]
        public void AdjustAgentAV_PositiveDelta_AddsItToCurrentAV()
        {
            FakeTurnAgent agent = Agent(100);
            _turnOrder.AddEntity(agent);

            _turnOrder.AdjustAgentAV(agent, 25f);

            Assert.That(_turnOrder.TurnQueue[0].CurrentAV, Is.EqualTo(125f).Within(Tolerance));
        }

        [Test]
        public void AdjustAgentAV_DeltaBelowOne_ClampsToOne()
        {
            FakeTurnAgent agent = Agent(100);
            _turnOrder.AddEntity(agent);

            _turnOrder.AdjustAgentAV(agent, -500f);

            Assert.That(_turnOrder.TurnQueue[0].CurrentAV, Is.EqualTo(1f).Within(Tolerance));
        }

        /// <summary>
        /// The "CurrentAV == 0f" guard. Only the agent whose turn is running sits at 0, so this is what
        /// keeps an AV effect from reshuffling the queue underneath the agent that is currently acting.
        /// </summary>
        [Test]
        public void AdjustAgentAV_ActiveAgentWithZeroAV_IsIgnored()
        {
            FakeTurnAgent active = Agent(200, "active");
            _turnOrder.AddEntity(active);
            _turnOrder.AddEntity(Agent(100, "other"));
            _turnOrder.StartActiveTurn();
            Assert.That(_turnOrder.TurnQueue[0].CurrentAV, Is.EqualTo(0f).Within(Tolerance),
                "Precondition: the active agent must be at AV 0.");
            _queueUpdatedCount = 0;

            _turnOrder.AdjustAgentAV(active, 500f);

            Assert.That(_turnOrder.TurnQueue[0].Agent, Is.SameAs(active));
            Assert.That(_turnOrder.TurnQueue[0].CurrentAV, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(_queueUpdatedCount, Is.EqualTo(0));
        }

        [Test]
        public void AdjustAgentAV_AgentNotInQueue_DoesNotRaiseQueueUpdatedEvent()
        {
            _turnOrder.AddEntity(Agent(100));
            _queueUpdatedCount = 0;

            _turnOrder.AdjustAgentAV(Agent(100, "stranger"), 25f);

            Assert.That(_queueUpdatedCount, Is.EqualTo(0));
        }

        // ---------------------------------------------------------------- Sorting / SendToBack

        /// <summary>
        /// The tie-break is only observable when two agents share an AV but not an agility, which cannot
        /// happen through AddEntity alone (AV is a function of agility). Here the second agent's agility is
        /// raised after joining and a no-op AdjustAgentAV forces the re-sort.
        /// </summary>
        [Test]
        public void SortQueue_EqualAV_OrdersByDescendingAgility()
        {
            FakeTurnAgent lessAgile = Agent(100, "lessAgile");
            FakeTurnAgent moreAgile = Agent(100, "moreAgile");
            _turnOrder.AddEntity(lessAgile);
            _turnOrder.AddEntity(moreAgile);
            moreAgile.EffectiveAgility = 200;

            _turnOrder.AdjustAgentAV(lessAgile, 0f); // same AV for both, triggers SortQueue

            Assert.That(_turnOrder.TurnQueue[0].CurrentAV,
                Is.EqualTo(_turnOrder.TurnQueue[1].CurrentAV).Within(Tolerance),
                "Precondition: both agents must still share the same AV.");
            Assert.That(_turnOrder.TurnQueue[0].Agent, Is.SameAs(moreAgile));
        }

        /// <summary>
        /// SendToBack assigns "highest AV in the queue + own base AV": 125 + 50 = 175 here, which puts the
        /// agent last no matter how agile it is.
        /// </summary>
        [Test]
        public void SendToBack_MiddleAgent_GivesItHighestAVAndLastPosition()
        {
            FakeTurnAgent fast = Agent(200, "fast");    // AV 50
            FakeTurnAgent middle = Agent(100, "mid");   // AV 100
            FakeTurnAgent slow = Agent(80, "slow");     // AV 125
            _turnOrder.AddEntity(fast);
            _turnOrder.AddEntity(middle);
            _turnOrder.AddEntity(slow);

            _turnOrder.SendToBack(fast);

            Assert.That(_turnOrder.TurnQueue[0].Agent, Is.SameAs(middle));
            Assert.That(_turnOrder.TurnQueue[1].Agent, Is.SameAs(slow));
            Assert.That(_turnOrder.TurnQueue[2].Agent, Is.SameAs(fast));
            Assert.That(_turnOrder.TurnQueue[2].CurrentAV, Is.EqualTo(175f).Within(Tolerance));
        }

        /// <summary>The max-AV scan skips the agent itself, so a lone agent gets 0 + its own base AV.</summary>
        [Test]
        public void SendToBack_SingleAgentQueue_SetsAVToBaseAV()
        {
            FakeTurnAgent agent = Agent(100);
            _turnOrder.AddEntity(agent);

            _turnOrder.SendToBack(agent);

            Assert.That(_turnOrder.TurnQueue[0].CurrentAV, Is.EqualTo(100f).Within(Tolerance));
        }

        [Test]
        public void SendToBack_AgentNotInQueue_DoesNotRaiseQueueUpdatedEvent()
        {
            _turnOrder.AddEntity(Agent(100));
            _queueUpdatedCount = 0;

            _turnOrder.SendToBack(Agent(100, "stranger"));

            Assert.That(_queueUpdatedCount, Is.EqualTo(0));
        }

        // ------------------------------------------------- agility changes moving an agent in the queue
        //
        // This is the production path end to end, minus the two links that are not C#: a passive changes the
        // agent's EffectiveAgility, HandlePassivesChanged turns that into an AV delta, and the delta reaches
        // AdjustAgentAV. The delta is computed here with StatUtils.BaseAVDelta — the very call the characters
        // make — so these tests exercise the real arithmetic rather than a restated copy of it.
        //
        // Note the game never calls UpdateAgentAV: it nudges by a delta, it does not recompute the countdown.

        /// <summary>
        /// Agilities 200 / 100 / 80 give AVs 50 / 100 / 125. Halving the middle agent's agility costs it a
        /// full extra base turn (delta 200 - 100 = +100), taking it to 200 and behind everyone.
        /// </summary>
        [Test]
        public void AdjustAgentAV_AgentSlowedByAnAgilityLoss_FallsToTheBackOfTheQueue()
        {
            FakeTurnAgent fast = Agent(200, "fast");     // AV 50
            FakeTurnAgent middle = Agent(100, "middle"); // AV 100
            FakeTurnAgent slow = Agent(80, "slow");      // AV 125
            _turnOrder.AddEntity(fast);
            _turnOrder.AddEntity(middle);
            _turnOrder.AddEntity(slow);

            float delta = StatUtils.BaseAVDelta(oldAgility: 100, newAgility: 50);
            middle.EffectiveAgility = 50; // the passive is live before the delta is emitted
            _turnOrder.AdjustAgentAV(middle, delta);

            Assert.That(_turnOrder.TurnQueue[0].Agent, Is.SameAs(fast));
            Assert.That(_turnOrder.TurnQueue[1].Agent, Is.SameAs(slow));
            Assert.That(_turnOrder.TurnQueue[2].Agent, Is.SameAs(middle));
            Assert.That(_turnOrder.TurnQueue[2].CurrentAV, Is.EqualTo(200f).Within(Tolerance));
        }

        /// <summary>
        /// The mirror case. Agilities 250 / 100 / 80 give AVs 40 / 100 / 125; quadrupling the middle agent's
        /// agility refunds it 75 AV (25 - 100), taking it to 25 and past the previous leader.
        /// </summary>
        [Test]
        public void AdjustAgentAV_AgentSpedUpByAnAgilityGain_MovesToTheFrontOfTheQueue()
        {
            FakeTurnAgent fast = Agent(250, "fast");     // AV 40
            FakeTurnAgent middle = Agent(100, "middle"); // AV 100
            FakeTurnAgent slow = Agent(80, "slow");      // AV 125
            _turnOrder.AddEntity(fast);
            _turnOrder.AddEntity(middle);
            _turnOrder.AddEntity(slow);

            float delta = StatUtils.BaseAVDelta(oldAgility: 100, newAgility: 400);
            middle.EffectiveAgility = 400;
            _turnOrder.AdjustAgentAV(middle, delta);

            Assert.That(_turnOrder.TurnQueue[0].Agent, Is.SameAs(middle));
            Assert.That(_turnOrder.TurnQueue[0].CurrentAV, Is.EqualTo(25f).Within(Tolerance));
            Assert.That(_turnOrder.TurnQueue[1].Agent, Is.SameAs(fast));
        }

        /// <summary>
        /// The guard against declaring victory on a reshuffle that happens for any reason: a penalty that does
        /// not close the gap must leave the order exactly as it was. 100 -> 80 costs 25 AV, and the agent
        /// behind sits 100 AV away.
        /// </summary>
        [Test]
        public void AdjustAgentAV_SlowTooSmallToCloseTheGap_LeavesTheOrderUnchanged()
        {
            FakeTurnAgent fast = Agent(200, "fast");     // AV 50
            FakeTurnAgent middle = Agent(100, "middle"); // AV 100
            FakeTurnAgent slow = Agent(50, "slow");      // AV 200
            _turnOrder.AddEntity(fast);
            _turnOrder.AddEntity(middle);
            _turnOrder.AddEntity(slow);

            float delta = StatUtils.BaseAVDelta(oldAgility: 100, newAgility: 80);
            middle.EffectiveAgility = 80;
            _turnOrder.AdjustAgentAV(middle, delta);

            Assert.That(_turnOrder.TurnQueue[0].Agent, Is.SameAs(fast));
            Assert.That(_turnOrder.TurnQueue[1].Agent, Is.SameAs(middle));
            Assert.That(_turnOrder.TurnQueue[2].Agent, Is.SameAs(slow));
            Assert.That(_turnOrder.TurnQueue[1].CurrentAV, Is.EqualTo(125f).Within(Tolerance));
        }

        /// <summary>
        /// The delta is sized on FULL base AVs but added to a countdown that has already partly elapsed, so
        /// the shift is not proportional to how close the agent was to acting.
        ///
        /// AVs 50 / 100 / 250; StartActiveTurn charges 50 to everyone, leaving the middle agent at 50 — half a
        /// turn away. Halving its agility then costs it the full 100, not the 50 a proportional penalty would
        /// have cost, landing it at 150.
        /// </summary>
        [Test]
        public void AdjustAgentAV_AfterStartActiveTurn_ShiftsByTheFullDeltaNotAProportionalOne()
        {
            FakeTurnAgent active = Agent(200, "active");   // AV 50
            FakeTurnAgent middle = Agent(100, "middle");   // AV 100
            FakeTurnAgent last = Agent(40, "last");        // AV 250
            _turnOrder.AddEntity(active);
            _turnOrder.AddEntity(middle);
            _turnOrder.AddEntity(last);

            _turnOrder.StartActiveTurn();
            Assert.That(_turnOrder.TurnQueue[1].CurrentAV, Is.EqualTo(50f).Within(Tolerance),
                "Precondition: the middle agent is half a turn from acting.");

            float delta = StatUtils.BaseAVDelta(oldAgility: 100, newAgility: 50);
            middle.EffectiveAgility = 50;
            _turnOrder.AdjustAgentAV(middle, delta);

            Assert.That(_turnOrder.TurnQueue[1].Agent, Is.SameAs(middle));
            Assert.That(_turnOrder.TurnQueue[1].CurrentAV, Is.EqualTo(150f).Within(Tolerance));
        }

        /// <summary>
        /// Slowing the agent whose turn is running does nothing to the queue: StartActiveTurn parks the head
        /// at 0 and AdjustAgentAV refuses to touch an agent at 0, so no effect can reshuffle the order from
        /// under the agent that is acting. The new agility is not lost though — CompleteActiveTurn recomputes
        /// the AV from scratch and picks it up then.
        /// </summary>
        [Test]
        public void AdjustAgentAV_ActiveAgentSlowed_IsDeferredUntilCompleteActiveTurn()
        {
            FakeTurnAgent active = Agent(200, "active");  // AV 50
            FakeTurnAgent other = Agent(100, "other");    // AV 100
            _turnOrder.AddEntity(active);
            _turnOrder.AddEntity(other);
            _turnOrder.StartActiveTurn();
            _queueUpdatedCount = 0;

            float delta = StatUtils.BaseAVDelta(oldAgility: 200, newAgility: 50);
            active.EffectiveAgility = 50;
            _turnOrder.AdjustAgentAV(active, delta);

            Assert.That(_turnOrder.TurnQueue[0].Agent, Is.SameAs(active), "The slow must not reorder mid-turn.");
            Assert.That(_turnOrder.TurnQueue[0].CurrentAV, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(_queueUpdatedCount, Is.EqualTo(0));

            _turnOrder.CompleteActiveTurn();

            Assert.That(_turnOrder.TurnQueue[1].Agent, Is.SameAs(active));
            Assert.That(_turnOrder.TurnQueue[1].CurrentAV, Is.EqualTo(200f).Within(Tolerance),
                "CompleteActiveTurn recomputes the base AV, so the new agility lands here.");
        }

        /// <summary>
        /// The order AddPassive produces when an elemental shield breaks: OnEquip runs first — and that is
        /// where ShieldBreakSlowPassiveSO calls SendToBack — then OnPassivesChanged emits the agility delta.
        /// The delta stacks on top of the banishment instead of undoing it, so the agent stays last. This is
        /// why the shipped asset keeps its penalty at zero: the drop to the back is meant to be the whole
        /// punishment.
        /// </summary>
        [Test]
        public void SendToBack_FollowedByAnAgilityPenalty_LeavesTheAgentLast()
        {
            FakeTurnAgent broken = Agent(200, "broken");  // AV 50
            FakeTurnAgent second = Agent(100, "second");  // AV 100
            FakeTurnAgent third = Agent(80, "third");     // AV 125
            _turnOrder.AddEntity(broken);
            _turnOrder.AddEntity(second);
            _turnOrder.AddEntity(third);

            _turnOrder.SendToBack(broken);
            Assert.That(_turnOrder.TurnQueue[2].Agent, Is.SameAs(broken), "Precondition: SendToBack ran first.");
            Assert.That(_turnOrder.TurnQueue[2].CurrentAV, Is.EqualTo(175f).Within(Tolerance)); // 125 + 50

            float delta = StatUtils.BaseAVDelta(oldAgility: 200, newAgility: 100);
            broken.EffectiveAgility = 100;
            _turnOrder.AdjustAgentAV(broken, delta);

            Assert.That(_turnOrder.TurnQueue[2].Agent, Is.SameAs(broken));
            Assert.That(_turnOrder.TurnQueue[2].CurrentAV, Is.EqualTo(225f).Within(Tolerance)); // 175 + 50
        }
    }
}
