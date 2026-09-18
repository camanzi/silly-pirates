using NUnit.Framework;

namespace SillyPirates.Tests.EditMode.Combat.Abilities.Character.Passive
{
    /// <summary>
    /// The speed buff. Its OnEquip dereferences the controller (TryGetComponent&lt;HostileCharacter&gt;), so these
    /// tests configure the bonuses directly rather than equipping — which is faithful anyway, because unlike
    /// the slow this passive's contribution does not depend on any equip-time state.
    /// </summary>
    public class SpeedBoostPassiveSOTests : ScriptableObjectFixture
    {
        private const float Tolerance = 0.001f;

        private SpeedBoostPassiveSO _boost;

        [SetUp]
        public void SetUp()
        {
            _boost = NewSO<SpeedBoostPassiveSO>();
            _boost.FlatBonus = 10;
            _boost.PercentBonus = 40f;
            _boost.DurationInTurns = 6;
        }

        /// <summary>
        /// The static HashSet of boosted enemies outlives a test and would carry destroyed objects into the
        /// next one. ResetForNewCombat is the production hook for exactly this.
        /// </summary>
        [TearDown]
        public void ClearSharedActiveTargets() => _boost.ResetForNewCombat();

        [Test]
        public void GetAgilityBonuses_WithoutEquipping_ReturnTheConfiguredBonuses()
        {
            IAgilityModifier modifier = _boost;

            Assert.That(modifier.GetFlatAgilityBonus(), Is.EqualTo(10));
            Assert.That(modifier.GetPercentageAgilityBonus(), Is.EqualTo(40f).Within(Tolerance));
        }

        /// <summary>
        /// The asymmetry with SlowPassiveSO worth pinning: the slow multiplies its penalty by its stack count,
        /// the boost has no stacks at all and stays flat however many times it is applied.
        /// </summary>
        [Test]
        public void GetAgilityBonuses_AreNotScaledByAnyStackCount()
        {
            Assert.That(_boost, Is.Not.InstanceOf<IStackablePassive>(),
                "The boost is deliberately not stackable; if that changes, this contract needs revisiting.");
            Assert.That(((IAgilityModifier)_boost).GetFlatAgilityBonus(), Is.EqualTo(10));
        }

        [Test]
        public void GetAgilityBonuses_AreSignedPositive_UnlikeASlow()
        {
            IAgilityModifier modifier = _boost;

            Assert.That(modifier.GetFlatAgilityBonus(), Is.Positive);
            Assert.That(modifier.GetPercentageAgilityBonus(), Is.Positive);
        }

        [Test]
        public void OnGlobalTurnEnd_FewerTicksThanDuration_DoesNotExpire()
        {
            _boost.RemovalTiming = PassiveRemovalTiming.OwnerTurnEnd;
            IOnGlobalTurnEnd ticking = _boost;

            for (int i = 0; i < 5; i++)
                ticking.OnGlobalTurnEnd();

            Assert.That(_boost.IsExpired, Is.False);
        }

        [Test]
        public void OnGlobalTurnEnd_ReachingTheDuration_MarksItExpired()
        {
            _boost.RemovalTiming = PassiveRemovalTiming.OwnerTurnEnd;
            IOnGlobalTurnEnd ticking = _boost;

            for (int i = 0; i < 6; i++)
                ticking.OnGlobalTurnEnd();

            Assert.That(_boost.IsExpired, Is.True);
        }
    }
}
