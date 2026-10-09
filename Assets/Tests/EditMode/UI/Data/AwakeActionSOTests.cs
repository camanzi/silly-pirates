using System;
using NUnit.Framework;
using UnityEngine;

namespace SillyPirates.Tests.EditMode.UI.Data
{
    /// <summary>
    /// The awaken action's hover preview. Spec (AwakeActionSO + IAwakable): the gain is 1 + the agent's
    /// TotalAwakeningBonus, and — now that overcap is gone and AddAwakeningPoints clamps at the max — the
    /// preview is clamped to Max(0, MaxAwakeningPoints - CurrentAwakeningPoints) so it never shows points
    /// that would be discarded.
    ///
    /// Runs against plain fakes: no ShipEquipment / EquipmentStateMachine is involved, so the clamp inside
    /// ShipEquipment.AddAwakeningPoints itself is NOT covered here.
    /// </summary>
    public class AwakeActionSOTests : ScriptableObjectFixture
    {
        private AwakeActionSO _action;

        [SetUp]
        public void SetUp() => _action = NewSO<AwakeActionSO>();

        // ---------------------------------------------------------------- fakes

        private class FakeElement : IInteractableElement
        {
            public Transform Transform => null;
            public OutlinerHelper OutlinerHelper => null;
            public SelectionContextSO SelectionContext => null;
            public InteractableElementEventChannel ClickChannel => null;
            public void OnHoverEnter() { }
            public void OnHoverExit() { }
            public void OnClick() { }
        }

        private sealed class FakeAwakable : FakeElement, IAwakable
        {
            public int MaxAwakeningPoints { get; set; }
            public int CurrentAwakeningPoints { get; set; }
            public bool IsAwake { get; set; }
            public bool IsOnCooldown { get; set; }
            public int Cooldown { get; set; }
            public void AddAwakeningPoints(int count) => CurrentAwakeningPoints += count;
            public void RemoveAwakeningPoints(int count) => CurrentAwakeningPoints -= count;
            public void ConsumeAllAwakeningPoints() => CurrentAwakeningPoints = 0;
            public Action OnAwakeningCountersChanged { get; set; }
            public Action<int> OnCooldownChanged { get; set; }
            public Action<int> OnAwakeningHoverPreview { get; set; }
        }

        private sealed class FakeBonusAgent : ITurnAgent, IAwakeningModifierHolder
        {
            public int TotalAwakeningBonus { get; set; }
            public void AddAwakeningModifier(IAwakeningModifier modifier) { }
            public void RemoveAwakeningModifier(IAwakeningModifier modifier) { }
            public void NotifyAwakeningContribution(IAwakable awakable) { }
            public event Action OnAwakeningModifiersChanged { add { } remove { } }

            public TurnRenderingAgentDataSO RenderingData => null;
            public TurnAgentDataSO AgentData => null;
            public TurnAgentEventChannel OnAgentJoin => null;
            public TurnAgentEventChannel OnAgentLeave => null;
            public IntEventChannel OnAPChanged => null;
            public InteractableProximityEventChannel ProximityChannel => null;
            public int RemainingActionPoints { get; set; }
            public int EffectiveAgility => 100;
            public HealthController Health => null;
            public string DisplayName => "BonusAgent";
            public void OnCombatJoin() { }
            public void OnCombatLeave() { }
            public void OnStartingTurn() { }
            public void OnContinuingTurn() { }
            public void OnEndingTurn() { }
            public bool CompareTag(string tag) => tag == TurnAgentRoles.CrewTag;
        }

        // ---------------------------------------------------------------- GetHoverAwakeningPreview

        /// <summary>
        /// Columns: bonus, max, current, expected. Expected = min(1 + bonus, max(0, max - current)).
        /// </summary>
        [TestCase(0, 5, 0, 1)] // plenty of room: the plain +1
        [TestCase(2, 5, 0, 3)] // plenty of room: 1 + bonus
        [TestCase(2, 5, 2, 3)] // exactly enough room
        [TestCase(2, 5, 3, 2)] // clamped to the 2 points left
        [TestCase(2, 5, 4, 1)] // clamped to the last point
        [TestCase(0, 5, 5, 0)] // full: nothing would be kept
        [TestCase(3, 5, 7, 0)] // already above max: Max(0, ...) floor, never negative
        public void GetHoverAwakeningPreview_AwakableTarget_ClampsToPointsLeftBeforeMax(
            int bonus, int max, int current, int expected)
        {
            FakeAwakable target = new FakeAwakable { MaxAwakeningPoints = max, CurrentAwakeningPoints = current };
            FakeBonusAgent agent = new FakeBonusAgent { TotalAwakeningBonus = bonus };

            int preview = _action.GetHoverAwakeningPreview(target, agent);

            Assert.That(preview, Is.EqualTo(expected));
        }

        /// <summary>An agent that is not an IAwakeningModifierHolder contributes no bonus: the gain is 1.</summary>
        [Test]
        public void GetHoverAwakeningPreview_AgentWithoutModifierHolder_PreviewsOnePoint()
        {
            FakeAwakable target = new FakeAwakable { MaxAwakeningPoints = 5, CurrentAwakeningPoints = 0 };

            int preview = _action.GetHoverAwakeningPreview(target, new FakeTurnAgent());

            Assert.That(preview, Is.EqualTo(1));
        }

        [Test]
        public void GetHoverAwakeningPreview_NullAgent_PreviewsOnePoint()
        {
            FakeAwakable target = new FakeAwakable { MaxAwakeningPoints = 5, CurrentAwakeningPoints = 0 };

            int preview = _action.GetHoverAwakeningPreview(target, null);

            Assert.That(preview, Is.EqualTo(1));
        }

        /// <summary>No IAwakable, nothing to clamp against: the raw 1 + bonus is returned.</summary>
        [Test]
        public void GetHoverAwakeningPreview_NonAwakableElement_ReturnsUnclampedGain()
        {
            FakeBonusAgent agent = new FakeBonusAgent { TotalAwakeningBonus = 2 };

            int preview = _action.GetHoverAwakeningPreview(new FakeElement(), agent);

            Assert.That(preview, Is.EqualTo(3));
        }
    }
}
