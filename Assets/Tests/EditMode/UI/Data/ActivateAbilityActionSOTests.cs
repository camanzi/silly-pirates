using System;
using NUnit.Framework;
using UnityEngine;

namespace SillyPirates.Tests.EditMode.UI.Data
{
    /// <summary>
    /// Readiness of the "activate ability" interaction. Spec (ActivateAbilityActionSO.CanExecute): only an
    /// IAwakable can be activated, only while awake, and never an IAutoActivatedEquipment — that equipment acts
    /// by itself and the crew only awakens it. Runs against plain fakes: CanExecute reads only these interfaces.
    /// </summary>
    public class ActivateAbilityActionSOTests : ScriptableObjectFixture
    {
        private ActivateAbilityActionSO _action;

        [SetUp]
        public void SetUp() => _action = NewSO<ActivateAbilityActionSO>();

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

        private class FakeAwakable : FakeElement, IAwakable
        {
            public int MaxAwakeningPoints { get; set; }
            public int CurrentAwakeningPoints { get; set; }
            public bool IsAwake { get; set; }
            public bool IsOnCooldown { get; set; }
            public int Cooldown { get; set; }
            public void AddAwakeningPoints(int count) { }
            public void RemoveAwakeningPoints(int count) { }
            public void ConsumeAllAwakeningPoints() { }
            public Action OnAwakeningCountersChanged { get; set; }
            public Action<int> OnCooldownChanged { get; set; }
            public Action<int> OnAwakeningHoverPreview { get; set; }
        }

        private sealed class FakeAutoActivatedAwakable : FakeAwakable, IAutoActivatedEquipment { }

        // ---------------------------------------------------------------- CanExecute

        [Test]
        public void CanExecute_AwakeAwakable_ReturnsTrue()
        {
            FakeAwakable element = new FakeAwakable { IsAwake = true };

            Assert.That(_action.CanExecute(element, new FakeTurnAgent()), Is.True);
        }

        [Test]
        public void CanExecute_AsleepAwakable_ReturnsFalse()
        {
            FakeAwakable element = new FakeAwakable { IsAwake = false };

            Assert.That(_action.CanExecute(element, new FakeTurnAgent()), Is.False);
        }

        [Test]
        public void CanExecute_AwakeAutoActivatedEquipment_ReturnsFalse()
        {
            FakeAutoActivatedAwakable element = new FakeAutoActivatedAwakable { IsAwake = true };

            Assert.That(_action.CanExecute(element, new FakeTurnAgent()), Is.False);
        }

        [Test]
        public void CanExecute_ElementThatIsNotAwakable_ReturnsFalse()
        {
            Assert.That(_action.CanExecute(new FakeElement(), new FakeTurnAgent()), Is.False);
        }
    }
}
