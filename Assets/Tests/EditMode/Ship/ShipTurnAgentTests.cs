using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SillyPirates.Tests.EditMode.Ship
{
    /// <summary>
    /// The ship's agility and its link to the ShipTurnRosterSO. Spec (ShipTurnAgent): EffectiveAgility is
    /// StatUtils.EvaluateAgility(base, passives + roster modifiers); a roster change raises an AV delta equal to
    /// StatUtils.BaseAVDelta(old, new) on the agility channel, and nothing when the value did not move;
    /// OnEndingTurn tells the roster's participants the ship has acted.
    ///
    /// The ship lives on an inactive GameObject and its Awake/OnEnable/Start are run by reflection (EditMode
    /// sends no lifecycle messages). Every SO is a fresh instance. Not covered: the turn itself
    /// (OnStartingTurn/RunTurnAsync awaits real seconds, PlayMode material) and the PassiveAbilityController
    /// path — the ship has none today.
    /// </summary>
    public class ShipTurnAgentTests : ScriptableObjectFixture
    {
        private const int BaseAgility = 100;
        private const float Tolerance = 0.001f;

        private ShipTurnRosterSO _roster;
        private AgentAVDeltaEventChannel _agilityChannel;
        private readonly List<AgentAVDeltaPayload> _raised = new();

        [SetUp]
        public void SetUp()
        {
            _roster = NewSO<ShipTurnRosterSO>();
            _agilityChannel = NewSO<AgentAVDeltaEventChannel>();
            _raised.Clear();
            _agilityChannel.OnEventRaised += CapturePayload;
        }

        [TearDown]
        public void TearDown()
        {
            if (_agilityChannel != null) _agilityChannel.OnEventRaised -= CapturePayload;
        }

        private void CapturePayload(AgentAVDeltaPayload payload) => _raised.Add(payload);

        /// <summary>Builds the ship and runs Awake → OnEnable → Start, the order Unity would use.</summary>
        private ShipTurnAgent NewShip(bool withRoster = true)
        {
            TurnAgentDataSO data = NewSO<TurnAgentDataSO>();
            SerializedObject dataSo = new SerializedObject(data);
            dataSo.FindProperty("_initialAgility").intValue = BaseAgility;
            dataSo.ApplyModifiedPropertiesWithoutUndo();

            ShipTurnAgent ship = NewInactiveGameObject("Ship").AddComponent<ShipTurnAgent>();
            SerializedObject shipSo = new SerializedObject(ship);
            shipSo.FindProperty("_agentData").objectReferenceValue = data;
            shipSo.FindProperty("_onAgilityChangedChannel").objectReferenceValue = _agilityChannel;
            shipSo.FindProperty("_shipTurnRoster").objectReferenceValue = withRoster ? _roster : null;
            shipSo.ApplyModifiedPropertiesWithoutUndo();

            LifecycleInvoker.Invoke(ship, typeof(ShipTurnAgent), "Awake");
            LifecycleInvoker.Invoke(ship, typeof(ShipTurnAgent), "OnEnable");
            LifecycleInvoker.Invoke(ship, typeof(ShipTurnAgent), "Start");
            return ship;
        }

        // ---------------------------------------------------------------- EffectiveAgility

        [Test]
        public void EffectiveAgility_EmptyRoster_EqualsBaseAgility()
        {
            ShipTurnAgent ship = NewShip();

            Assert.That(ship.EffectiveAgility, Is.EqualTo(BaseAgility));
        }

        [Test]
        public void EffectiveAgility_NoRosterAssigned_EqualsBaseAgility()
        {
            ShipTurnAgent ship = NewShip(withRoster: false);

            Assert.That(ship.EffectiveAgility, Is.EqualTo(BaseAgility));
        }

        [Test]
        public void EffectiveAgility_OneBoosterInRoster_AddsItsFlatBonus()
        {
            ShipTurnAgent ship = NewShip();

            _roster.Register(new FakeAgilityBoosterParticipant(flat: 20));

            Assert.That(ship.EffectiveAgility, Is.EqualTo(120));
        }

        /// <summary>(100 + 10 + 20) * (1 + (10 + 20)/100) = 130 * 1.3 = 169: flats sum, percentages sum.</summary>
        [Test]
        public void EffectiveAgility_TwoBoosters_SumsFlatsThenAppliesSummedPercentages()
        {
            ShipTurnAgent ship = NewShip();

            _roster.Register(new FakeAgilityBoosterParticipant(flat: 10, percentage: 10f));
            _roster.Register(new FakeAgilityBoosterParticipant(flat: 20, percentage: 20f));

            Assert.That(ship.EffectiveAgility, Is.EqualTo(169));
        }

        [Test]
        public void EffectiveAgility_ParticipantThatIsNotAModifier_IsIgnored()
        {
            ShipTurnAgent ship = NewShip();

            _roster.Register(new FakeShipTurnParticipant());

            Assert.That(ship.EffectiveAgility, Is.EqualTo(BaseAgility));
        }

        /// <summary>
        /// CollectModifiers appends, and the ship reuses one list: without the clear on every read the
        /// booster would be counted again each time the getter runs.
        /// </summary>
        [Test]
        public void EffectiveAgility_ReadRepeatedly_DoesNotAccumulateModifiers()
        {
            ShipTurnAgent ship = NewShip();
            _roster.Register(new FakeAgilityBoosterParticipant(flat: 20));

            int first = ship.EffectiveAgility;
            int second = ship.EffectiveAgility;
            int third = ship.EffectiveAgility;

            Assert.That(new[] { first, second, third }, Is.EqualTo(new[] { 120, 120, 120 }));
        }

        // ---------------------------------------------------------------- AV delta on roster change

        [Test]
        public void RosterChanged_BoosterRegistered_RaisesDeltaEqualToBaseAVDelta()
        {
            ShipTurnAgent ship = NewShip();

            _roster.Register(new FakeAgilityBoosterParticipant(flat: 20));

            Assert.That(_raised.Count, Is.EqualTo(1));
            Assert.That(_raised[0].AVDelta, Is.EqualTo(StatUtils.BaseAVDelta(100, 120)).Within(Tolerance));
            Assert.That(_raised[0].Agent, Is.SameAs(ship));
        }

        /// <summary>A sped-up ship needs less AV per turn: the delta is negative.</summary>
        [Test]
        public void RosterChanged_BoosterRegistered_DeltaIsNegative()
        {
            NewShip();

            _roster.Register(new FakeAgilityBoosterParticipant(flat: 20));

            Assert.That(_raised[0].AVDelta, Is.LessThan(0f));
        }

        [Test]
        public void RosterChanged_BoosterUnregistered_RaisesDeltaBackToBase()
        {
            NewShip();
            FakeAgilityBoosterParticipant booster = new FakeAgilityBoosterParticipant(flat: 20);
            _roster.Register(booster);
            _raised.Clear();

            _roster.Unregister(booster);

            Assert.That(_raised.Count, Is.EqualTo(1));
            Assert.That(_raised[0].AVDelta, Is.EqualTo(StatUtils.BaseAVDelta(120, 100)).Within(Tolerance));
        }

        /// <summary>Two consecutive changes: the second delta starts from the agility the first one left.</summary>
        [Test]
        public void RosterChanged_SecondBoosterRegistered_DeltaStartsFromThePreviousAgility()
        {
            NewShip();
            _roster.Register(new FakeAgilityBoosterParticipant(flat: 20));
            _raised.Clear();

            _roster.Register(new FakeAgilityBoosterParticipant(flat: 30));

            Assert.That(_raised.Count, Is.EqualTo(1));
            Assert.That(_raised[0].AVDelta, Is.EqualTo(StatUtils.BaseAVDelta(120, 150)).Within(Tolerance));
        }

        [Test]
        public void RosterChanged_ParticipantThatIsNotAModifier_RaisesNothing()
        {
            NewShip();

            _roster.Register(new FakeShipTurnParticipant());

            Assert.That(_raised, Is.Empty);
        }

        [Test]
        public void RosterChanged_ZeroBonusBooster_RaisesNothing()
        {
            NewShip();

            _roster.Register(new FakeAgilityBoosterParticipant());

            Assert.That(_raised, Is.Empty);
        }

        [Test]
        public void RosterChanged_AfterOnDisable_RaisesNothing()
        {
            ShipTurnAgent ship = NewShip();
            LifecycleInvoker.Invoke(ship, typeof(ShipTurnAgent), "OnDisable");

            _roster.Register(new FakeAgilityBoosterParticipant(flat: 20));

            Assert.That(_raised, Is.Empty);
        }

        // ---------------------------------------------------------------- OnEndingTurn

        [Test]
        public void OnEndingTurn_RosterWithParticipants_NotifiesEachOnce()
        {
            ShipTurnAgent ship = NewShip();
            FakeShipTurnParticipant first = new FakeShipTurnParticipant();
            FakeAgilityBoosterParticipant second = new FakeAgilityBoosterParticipant(flat: 20);
            _roster.Register(first);
            _roster.Register(second);

            ship.OnEndingTurn();

            Assert.That(first.ShipTurnEndedCalls, Is.EqualTo(1));
            Assert.That(second.ShipTurnEndedCalls, Is.EqualTo(1));
        }

        [Test]
        public void OnEndingTurn_NoRosterAssigned_DoesNotThrow()
        {
            ShipTurnAgent ship = NewShip(withRoster: false);

            Assert.That(() => ship.OnEndingTurn(), Throws.Nothing);
        }

        /// <summary>
        /// The whole loop with a booster that, like the real one, leaves the roster when told the ship has acted:
        /// the ship is back at its base agility and the delta that undoes the bonus is raised.
        /// </summary>
        [Test]
        public void OnEndingTurn_BoosterLeavesTheRoster_AgilityReturnsToBaseAndDeltaIsRaised()
        {
            ShipTurnAgent ship = NewShip();
            FakeAgilityBoosterParticipant booster = new FakeAgilityBoosterParticipant(flat: 20);
            booster.OnTurnEnded = () => _roster.Unregister(booster);
            _roster.Register(booster);
            _raised.Clear();

            ship.OnEndingTurn();

            Assert.That(ship.EffectiveAgility, Is.EqualTo(BaseAgility));
            Assert.That(_raised.Count, Is.EqualTo(1));
            Assert.That(_raised[0].AVDelta, Is.EqualTo(StatUtils.BaseAVDelta(120, 100)).Within(Tolerance));
        }
    }
}
