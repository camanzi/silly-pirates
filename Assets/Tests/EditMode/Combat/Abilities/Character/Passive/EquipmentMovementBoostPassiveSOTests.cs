using NUnit.Framework;

namespace SillyPirates.Tests.EditMode.Combat.Abilities.Character.Passive
{
    /// <summary>
    /// The movement bonus granted by awakening equipment, and its stack ceiling.
    ///
    /// The tests drive IStackablePassive.OnReapplied, which forwards to the same OnEquipmentAwakened the event
    /// channel would call. OnEquip is avoided on purpose: it subscribes to _equipmentAwakenedChannel without a
    /// null guard, and a CreateInstance has no channel assigned.
    /// </summary>
    public class EquipmentMovementBoostPassiveSOTests : ScriptableObjectFixture
    {
        private EquipmentMovementBoostPassiveSO _boost;

        [SetUp]
        public void SetUp() => _boost = NewSO<EquipmentMovementBoostPassiveSO>();

        private void Awaken(int times)
        {
            IStackablePassive stackable = _boost;
            for (int i = 0; i < times; i++)
                stackable.OnReapplied(null, _boost);
        }

        [Test]
        public void CurrentStacks_FreshInstance_IsZeroAndGrantsNoMovement()
        {
            Assert.That(_boost.CurrentStacks, Is.EqualTo(0));
            Assert.That(_boost.GetMovementBonus(), Is.EqualTo(0));
        }

        [Test]
        public void OnReapplied_EachAwakening_AddsOneStackOfMovement()
        {
            Awaken(1);
            Assert.That(_boost.GetMovementBonus(), Is.EqualTo(1));

            Awaken(1);
            Assert.That(_boost.GetMovementBonus(), Is.EqualTo(2));
        }

        /// <summary>
        /// The guard used to hardcode its own 3 next to MaxStacks => 3. Two constants for one rule meant the
        /// HUD could advertise a cap the code did not enforce; this test is what keeps them tied together.
        /// </summary>
        [Test]
        public void OnReapplied_BeyondTheCap_StopsAtMaxStacks()
        {
            Awaken(_boost.MaxStacks + 5);

            Assert.That(_boost.CurrentStacks, Is.EqualTo(_boost.MaxStacks));
            Assert.That(_boost.GetMovementBonus(), Is.EqualTo(_boost.MaxStacks));
        }

        [Test]
        public void CurrentStacks_NeverExceedsMaxStacks()
        {
            for (int i = 0; i < _boost.MaxStacks + 3; i++)
            {
                Awaken(1);
                Assert.That(_boost.CurrentStacks, Is.LessThanOrEqualTo(_boost.MaxStacks));
            }
        }

        /// <summary>IsCurrentlyActive drives whether the icon is rendered at all in the crew HUD.</summary>
        [Test]
        public void IsCurrentlyActive_OnlyOnceAtLeastOneStackIsHeld()
        {
            Assert.That(_boost.IsCurrentlyActive(null), Is.False);

            Awaken(1);

            Assert.That(_boost.IsCurrentlyActive(null), Is.True);
        }
    }
}
