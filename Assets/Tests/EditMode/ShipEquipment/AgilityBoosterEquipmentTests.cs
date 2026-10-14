using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace SillyPirates.Tests.EditMode.ShipEquipment
{
    /// <summary>
    /// The agility booster and its wiring into the ShipTurnRosterSO (ShipEquipment's OnActiveChanged hook).
    /// Spec: bonuses come from AgilityBoosterStatsSO (0 without stats); reaching the awakening threshold puts it
    /// in the roster; OnShipTurnEnded on an awake booster spends every point and starts the stats' cooldown (1
    /// without stats), which takes it out of the roster; undoing the awakening takes it out too.
    ///
    /// Rig: the real component with its real EquipmentStateMachine on an inactive GameObject; Awake / OnEnable /
    /// Start run by reflection in Unity's order. GridElement.OnEnable looks for a floor that does not exist in
    /// EditMode and logs an error on that intended path, so every OnEnable expects it.
    /// </summary>
    public class AgilityBoosterEquipmentTests : ScriptableObjectFixture
    {
        private const int ToAwakePoints = 3;
        private static readonly Regex NoFloorError = new Regex("no floor found");

        private ShipTurnRosterSO _roster;

        [SetUp]
        public void SetUp() => _roster = NewSO<ShipTurnRosterSO>();

        private AgilityBoosterStatsSO NewStats(int flat, float percentage, int cooldownTurns)
        {
            AgilityBoosterStatsSO stats = NewSO<AgilityBoosterStatsSO>();
            SerializedObject so = new SerializedObject(stats);
            so.FindProperty("_flatAgilityBonus").intValue = flat;
            so.FindProperty("_percentageAgilityBonus").floatValue = percentage;
            so.FindProperty("_cooldownTurns").intValue = cooldownTurns;
            so.ApplyModifiedPropertiesWithoutUndo();
            return stats;
        }

        /// <summary>Configures the booster and runs the lifecycle: Awake (both), OnEnable, Start (state machine).</summary>
        private AgilityBoosterEquipment NewBooster(AgilityBoosterStatsSO stats)
        {
            AgilityBoosterEquipment booster = NewInactiveGameObject("Booster").AddComponent<AgilityBoosterEquipment>();
            EquipmentStateMachine stateMachine = booster.GetComponent<EquipmentStateMachine>();
            Assert.That(stateMachine, Is.Not.Null, "RequireComponent should have added the EquipmentStateMachine");

            SerializedObject so = new SerializedObject(booster);
            so.FindProperty("_statsConfig").objectReferenceValue = stats;
            so.FindProperty("_toAwakePoints").intValue = ToAwakePoints;
            so.FindProperty("_shipTurnRoster").objectReferenceValue = _roster;
            so.ApplyModifiedPropertiesWithoutUndo();

            LifecycleInvoker.Invoke(booster, typeof(global::ShipEquipment), "Awake");
            LifecycleInvoker.Invoke(stateMachine, typeof(EquipmentStateMachine), "Awake");
            Enable(booster);
            LifecycleInvoker.Invoke(stateMachine, typeof(EquipmentStateMachine), "Start");
            return booster;
        }

        private static void Enable(AgilityBoosterEquipment booster)
        {
            LogAssert.Expect(LogType.Error, NoFloorError);
            LifecycleInvoker.Invoke(booster, typeof(global::ShipEquipment), "OnEnable");
        }

        private static void Disable(AgilityBoosterEquipment booster) =>
            LifecycleInvoker.Invoke(booster, typeof(global::ShipEquipment), "OnDisable");

        // ---------------------------------------------------------------- Bonuses

        [Test]
        public void GetFlatAgilityBonus_WithStats_ReturnsTheStatsValue()
        {
            AgilityBoosterEquipment booster = NewBooster(NewStats(flat: 25, percentage: 0f, cooldownTurns: 2));

            Assert.That(booster.GetFlatAgilityBonus(), Is.EqualTo(25));
        }

        [Test]
        public void GetPercentageAgilityBonus_WithStats_ReturnsTheStatsValue()
        {
            AgilityBoosterEquipment booster = NewBooster(NewStats(flat: 0, percentage: 15f, cooldownTurns: 2));

            Assert.That(booster.GetPercentageAgilityBonus(), Is.EqualTo(15f).Within(0.0001f));
        }

        [Test]
        public void GetFlatAgilityBonus_WithoutStats_ReturnsZero()
        {
            AgilityBoosterEquipment booster = NewBooster(null);

            Assert.That(booster.GetFlatAgilityBonus(), Is.EqualTo(0));
        }

        [Test]
        public void GetPercentageAgilityBonus_WithoutStats_ReturnsZero()
        {
            AgilityBoosterEquipment booster = NewBooster(null);

            Assert.That(booster.GetPercentageAgilityBonus(), Is.EqualTo(0f).Within(0.0001f));
        }

        // ---------------------------------------------------------------- Awakening ↔ roster

        [Test]
        public void Start_FreshBooster_IsNotInTheRoster()
        {
            AgilityBoosterEquipment booster = NewBooster(NewStats(20, 0f, 2));

            Assert.That(booster.IsAwake, Is.False);
            Assert.That(_roster.Contains(booster), Is.False);
        }

        [Test]
        public void AddAwakeningPoints_BelowThreshold_DoesNotRegister()
        {
            AgilityBoosterEquipment booster = NewBooster(NewStats(20, 0f, 2));

            booster.AddAwakeningPoints(ToAwakePoints - 1);

            Assert.That(_roster.Contains(booster), Is.False);
        }

        [Test]
        public void AddAwakeningPoints_ReachingThreshold_RegistersInTheRoster()
        {
            AgilityBoosterEquipment booster = NewBooster(NewStats(20, 0f, 2));

            booster.AddAwakeningPoints(ToAwakePoints);

            Assert.That(booster.IsAwake, Is.True);
            Assert.That(_roster.Contains(booster), Is.True);
        }

        [Test]
        public void RemoveAwakeningPoints_UndoingTheAwakening_UnregistersFromTheRoster()
        {
            AgilityBoosterEquipment booster = NewBooster(NewStats(20, 0f, 2));
            booster.AddAwakeningPoints(ToAwakePoints);

            booster.RemoveAwakeningPoints(1);

            Assert.That(booster.IsAwake, Is.False);
            Assert.That(_roster.Contains(booster), Is.False);
        }

        [Test]
        public void OnDisable_WhileAwake_UnregistersFromTheRoster()
        {
            AgilityBoosterEquipment booster = NewBooster(NewStats(20, 0f, 2));
            booster.AddAwakeningPoints(ToAwakePoints);

            Disable(booster);

            Assert.That(_roster.Contains(booster), Is.False);
        }

        [Test]
        public void OnEnable_ReEnabledWhileStillAwake_RegistersAgain()
        {
            AgilityBoosterEquipment booster = NewBooster(NewStats(20, 0f, 2));
            booster.AddAwakeningPoints(ToAwakePoints);
            Disable(booster);

            Enable(booster);

            Assert.That(_roster.Contains(booster), Is.True);
        }

        /// <summary>OnDisable unsubscribes from OnActiveChanged: a disabled booster that awakens stays out.</summary>
        [Test]
        public void AddAwakeningPoints_AfterOnDisable_DoesNotRegister()
        {
            AgilityBoosterEquipment booster = NewBooster(NewStats(20, 0f, 2));
            Disable(booster);

            booster.AddAwakeningPoints(ToAwakePoints);

            Assert.That(_roster.Contains(booster), Is.False);
        }

        // ---------------------------------------------------------------- OnShipTurnEnded

        [Test]
        public void OnShipTurnEnded_Awake_StartsTheCooldownFromStats()
        {
            AgilityBoosterEquipment booster = NewBooster(NewStats(20, 0f, cooldownTurns: 3));
            booster.AddAwakeningPoints(ToAwakePoints);

            booster.OnShipTurnEnded();

            Assert.That(booster.IsOnCooldown, Is.True);
            Assert.That(booster.Cooldown, Is.EqualTo(3));
        }

        [Test]
        public void OnShipTurnEnded_Awake_ConsumesEveryAwakeningPoint()
        {
            AgilityBoosterEquipment booster = NewBooster(NewStats(20, 0f, 2));
            booster.AddAwakeningPoints(ToAwakePoints);

            booster.OnShipTurnEnded();

            Assert.That(booster.CurrentAwakeningPoints, Is.EqualTo(0));
        }

        [Test]
        public void OnShipTurnEnded_Awake_UnregistersFromTheRoster()
        {
            AgilityBoosterEquipment booster = NewBooster(NewStats(20, 0f, 2));
            booster.AddAwakeningPoints(ToAwakePoints);

            booster.OnShipTurnEnded();

            Assert.That(booster.IsAwake, Is.False);
            Assert.That(_roster.Contains(booster), Is.False);
        }

        [Test]
        public void OnShipTurnEnded_AwakeWithoutStats_CooldownIsOneTurn()
        {
            AgilityBoosterEquipment booster = NewBooster(null);
            booster.AddAwakeningPoints(ToAwakePoints);

            booster.OnShipTurnEnded();

            Assert.That(booster.IsOnCooldown, Is.True);
            Assert.That(booster.Cooldown, Is.EqualTo(1));
        }

        [Test]
        public void OnShipTurnEnded_NotAwake_LeavesPointsAndStateUntouched()
        {
            AgilityBoosterEquipment booster = NewBooster(NewStats(20, 0f, 2));
            booster.AddAwakeningPoints(ToAwakePoints - 1);

            booster.OnShipTurnEnded();

            Assert.That(booster.CurrentAwakeningPoints, Is.EqualTo(ToAwakePoints - 1));
            Assert.That(booster.IsOnCooldown, Is.False);
        }

        /// <summary>The real path: the roster notifies, the booster leaves the roster from inside the call.</summary>
        [Test]
        public void RosterNotifyShipTurnEnded_TwoAwakeBoosters_BothGoOnCooldownAndLeaveTheRoster()
        {
            AgilityBoosterEquipment first = NewBooster(NewStats(20, 0f, 2));
            AgilityBoosterEquipment second = NewBooster(NewStats(10, 0f, 2));
            first.AddAwakeningPoints(ToAwakePoints);
            second.AddAwakeningPoints(ToAwakePoints);

            _roster.NotifyShipTurnEnded();

            Assert.That(first.IsOnCooldown, Is.True, "first booster");
            Assert.That(second.IsOnCooldown, Is.True, "second booster");
            Assert.That(_roster.Participants, Is.Empty);
        }
    }
}
