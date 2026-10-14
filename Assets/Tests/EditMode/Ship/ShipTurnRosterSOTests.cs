using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SillyPirates.Tests.EditMode.Ship
{
    /// <summary>
    /// The roster of awake equipment tied to the ship's turn. Spec (ShipTurnRosterSO): Register is idempotent and
    /// null-safe, Unregister raises only when something was removed, CollectModifiers appends without clearing,
    /// NotifyShipTurnEnded walks a snapshot and isolates a throwing participant, ResetForNewCombat clears and
    /// raises. A fresh roster per test: the project asset is a CombatSession resettable and must never be touched.
    /// </summary>
    public class ShipTurnRosterSOTests : ScriptableObjectFixture
    {
        private ShipTurnRosterSO _roster;
        private int _changedCount;

        [SetUp]
        public void SetUp()
        {
            _roster = NewSO<ShipTurnRosterSO>();
            _changedCount = 0;
            _roster.OnRosterChanged += CountChange;
        }

        [TearDown]
        public void TearDown()
        {
            if (_roster != null) _roster.OnRosterChanged -= CountChange;
        }

        private void CountChange() => _changedCount++;

        // ---------------------------------------------------------------- Register

        [Test]
        public void Register_NewParticipant_AddsItAndRaisesOnce()
        {
            FakeShipTurnParticipant participant = new FakeShipTurnParticipant();

            _roster.Register(participant);

            Assert.That(_roster.Participants, Is.EqualTo(new[] { participant }));
            Assert.That(_changedCount, Is.EqualTo(1));
        }

        [Test]
        public void Register_SameParticipantTwice_KeepsOneEntryAndRaisesOnce()
        {
            FakeShipTurnParticipant participant = new FakeShipTurnParticipant();

            _roster.Register(participant);
            _roster.Register(participant);

            Assert.That(_roster.Participants.Count, Is.EqualTo(1));
            Assert.That(_changedCount, Is.EqualTo(1));
        }

        [Test]
        public void Register_Null_AddsNothingAndDoesNotRaise()
        {
            _roster.Register(null);

            Assert.That(_roster.Participants, Is.Empty);
            Assert.That(_changedCount, Is.EqualTo(0));
        }

        [Test]
        public void Register_TwoParticipants_KeepsRegistrationOrder()
        {
            FakeShipTurnParticipant first = new FakeShipTurnParticipant();
            FakeShipTurnParticipant second = new FakeShipTurnParticipant();

            _roster.Register(first);
            _roster.Register(second);

            Assert.That(_roster.Participants, Is.EqualTo(new[] { first, second }));
        }

        // ---------------------------------------------------------------- Unregister / Contains

        [Test]
        public void Unregister_RegisteredParticipant_RemovesItAndRaises()
        {
            FakeShipTurnParticipant participant = new FakeShipTurnParticipant();
            _roster.Register(participant);
            _changedCount = 0;

            _roster.Unregister(participant);

            Assert.That(_roster.Contains(participant), Is.False);
            Assert.That(_changedCount, Is.EqualTo(1));
        }

        [Test]
        public void Unregister_AbsentParticipant_DoesNotRaise()
        {
            _roster.Register(new FakeShipTurnParticipant());
            _changedCount = 0;

            _roster.Unregister(new FakeShipTurnParticipant());

            Assert.That(_changedCount, Is.EqualTo(0));
            Assert.That(_roster.Participants.Count, Is.EqualTo(1));
        }

        [Test]
        public void Unregister_Null_DoesNotRaise()
        {
            _roster.Unregister(null);

            Assert.That(_changedCount, Is.EqualTo(0));
        }

        [Test]
        public void Contains_RegisteredAndUnknownParticipant_TellsThemApart()
        {
            FakeShipTurnParticipant registered = new FakeShipTurnParticipant();
            _roster.Register(registered);

            Assert.That(_roster.Contains(registered), Is.True);
            Assert.That(_roster.Contains(new FakeShipTurnParticipant()), Is.False);
        }

        // ---------------------------------------------------------------- CollectModifiers

        /// <summary>The caller merges passives first and roster modifiers after: the list must not be cleared.</summary>
        [Test]
        public void CollectModifiers_NonEmptyList_AppendsWithoutClearing()
        {
            FakeAgilityModifier existing = new FakeAgilityModifier(flat: 5);
            FakeAgilityBoosterParticipant booster = new FakeAgilityBoosterParticipant(flat: 20);
            _roster.Register(booster);
            List<IAgilityModifier> results = new List<IAgilityModifier> { existing };

            _roster.CollectModifiers(results);

            Assert.That(results, Is.EqualTo(new IAgilityModifier[] { existing, booster }));
        }

        [Test]
        public void CollectModifiers_MixedParticipants_CollectsOnlyThoseOfTheRequestedType()
        {
            FakeAgilityBoosterParticipant first = new FakeAgilityBoosterParticipant(flat: 10);
            FakeShipTurnParticipant notAModifier = new FakeShipTurnParticipant();
            FakeAgilityBoosterParticipant second = new FakeAgilityBoosterParticipant(flat: 20);
            _roster.Register(first);
            _roster.Register(notAModifier);
            _roster.Register(second);
            List<IAgilityModifier> results = new List<IAgilityModifier>();

            _roster.CollectModifiers(results);

            Assert.That(results, Is.EqualTo(new IAgilityModifier[] { first, second }));
        }

        [Test]
        public void CollectModifiers_EmptyRoster_LeavesTheListUntouched()
        {
            FakeAgilityModifier existing = new FakeAgilityModifier(flat: 5);
            List<IAgilityModifier> results = new List<IAgilityModifier> { existing };

            _roster.CollectModifiers(results);

            Assert.That(results, Is.EqualTo(new IAgilityModifier[] { existing }));
        }

        // ---------------------------------------------------------------- NotifyShipTurnEnded

        [Test]
        public void NotifyShipTurnEnded_TwoParticipants_CallsEachExactlyOnce()
        {
            FakeShipTurnParticipant first = new FakeShipTurnParticipant();
            FakeShipTurnParticipant second = new FakeShipTurnParticipant();
            _roster.Register(first);
            _roster.Register(second);

            _roster.NotifyShipTurnEnded();

            Assert.That(first.ShipTurnEndedCalls, Is.EqualTo(1));
            Assert.That(second.ShipTurnEndedCalls, Is.EqualTo(1));
        }

        /// <summary>
        /// What a real booster does: going on cooldown removes it from the roster during the call. Iterating the
        /// live list would skip the participant that slid into the removed slot.
        /// </summary>
        [Test]
        public void NotifyShipTurnEnded_ParticipantUnregistersItselfDuringTheCall_StillCallsEveryParticipantOnce()
        {
            FakeShipTurnParticipant first = new FakeShipTurnParticipant();
            FakeShipTurnParticipant second = new FakeShipTurnParticipant();
            first.OnTurnEnded = () => _roster.Unregister(first);
            second.OnTurnEnded = () => _roster.Unregister(second);
            _roster.Register(first);
            _roster.Register(second);

            _roster.NotifyShipTurnEnded();

            Assert.That(first.ShipTurnEndedCalls, Is.EqualTo(1));
            Assert.That(second.ShipTurnEndedCalls, Is.EqualTo(1));
            Assert.That(_roster.Participants, Is.Empty);
        }

        [Test]
        public void NotifyShipTurnEnded_ParticipantThrows_LogsTheExceptionAndCallsTheRest()
        {
            FakeShipTurnParticipant throwing = new FakeShipTurnParticipant
            {
                OnTurnEnded = () => throw new InvalidOperationException("participant failure under test")
            };
            FakeShipTurnParticipant after = new FakeShipTurnParticipant();
            _roster.Register(throwing);
            _roster.Register(after);
            LogAssert.Expect(LogType.Exception, new Regex("participant failure under test"));

            Assert.That(() => _roster.NotifyShipTurnEnded(), Throws.Nothing);

            Assert.That(after.ShipTurnEndedCalls, Is.EqualTo(1));
        }

        [Test]
        public void NotifyShipTurnEnded_EmptyRoster_DoesNotThrow()
        {
            Assert.That(() => _roster.NotifyShipTurnEnded(), Throws.Nothing);
        }

        // ---------------------------------------------------------------- ResetForNewCombat

        [Test]
        public void ResetForNewCombat_WithParticipants_ClearsAndRaises()
        {
            _roster.Register(new FakeShipTurnParticipant());
            _roster.Register(new FakeShipTurnParticipant());
            _changedCount = 0;

            _roster.ResetForNewCombat();

            Assert.That(_roster.Participants, Is.Empty);
            Assert.That(_changedCount, Is.EqualTo(1));
        }
    }
}
