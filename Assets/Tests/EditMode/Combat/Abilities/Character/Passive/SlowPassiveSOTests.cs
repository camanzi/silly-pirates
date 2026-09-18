using NUnit.Framework;

namespace SillyPirates.Tests.EditMode.Combat.Abilities.Character.Passive
{
    /// <summary>
    /// The slow debuff: how its agility penalty scales with stacks, and how its duration runs out.
    ///
    /// OnEquip(null) is safe here — unlike its two siblings this one only writes its own fields, and
    /// PlayApplyVFX returns immediately because a CreateInstance has no VFX prefab or channel assigned.
    /// That is what lets these tests exercise the real stacking path without a PassiveAbilityController,
    /// whose AddPassive calls Destroy and therefore cannot run in EditMode.
    /// </summary>
    public class SlowPassiveSOTests : ScriptableObjectFixture
    {
        private const float Tolerance = 0.001f;

        private SlowPassiveSO _slow;

        [SetUp]
        public void SetUp()
        {
            _slow = NewSO<SlowPassiveSO>();
            _slow.FlatPenalty = 10;
            _slow.PercentPenalty = 20f;
            _slow.DurationInTurns = 3;
        }

        /// <summary>
        /// _stacks is set to 1 by OnEquip and starts at 0, so an instance that was never equipped is a slow
        /// that does not slow. Anything building one by hand has to equip it.
        /// </summary>
        [Test]
        public void GetAgilityBonuses_NeverEquipped_ContributeNothing()
        {
            IAgilityModifier modifier = _slow;

            Assert.That(_slow.CurrentStacks, Is.EqualTo(0), "Precondition: a fresh instance carries no stacks.");
            Assert.That(modifier.GetFlatAgilityBonus(), Is.EqualTo(0));
            Assert.That(modifier.GetPercentageAgilityBonus(), Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void OnEquip_FreshInstance_AppliesOneStackWorthOfPenalty()
        {
            _slow.OnEquip(null);

            IAgilityModifier modifier = _slow;
            Assert.That(_slow.CurrentStacks, Is.EqualTo(1));
            Assert.That(modifier.GetFlatAgilityBonus(), Is.EqualTo(-10));
            Assert.That(modifier.GetPercentageAgilityBonus(), Is.EqualTo(-20f).Within(Tolerance));
        }

        /// <summary>Both terms scale with the stack count, not just the flat one.</summary>
        [Test]
        public void OnReapplied_TwiceAfterEquip_TriplesBothPenalties()
        {
            _slow.OnEquip(null);
            IStackablePassive stackable = _slow;

            stackable.OnReapplied(null, _slow);
            stackable.OnReapplied(null, _slow);

            IAgilityModifier modifier = _slow;
            Assert.That(_slow.CurrentStacks, Is.EqualTo(3));
            Assert.That(modifier.GetFlatAgilityBonus(), Is.EqualTo(-30));
            Assert.That(modifier.GetPercentageAgilityBonus(), Is.EqualTo(-60f).Within(Tolerance));
        }

        /// <summary>The slow stacks without a ceiling, which is what MaxStacks == int.MaxValue declares.</summary>
        [Test]
        public void MaxStacks_IsUnbounded()
        {
            Assert.That(_slow.MaxStacks, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void OnEquip_AfterStacking_ResetsToASingleStack()
        {
            _slow.OnEquip(null);
            ((IStackablePassive)_slow).OnReapplied(null, _slow);

            _slow.OnEquip(null);

            Assert.That(_slow.CurrentStacks, Is.EqualTo(1));
        }

        // ---------------------------------------------------------------- duration

        /// <summary>
        /// The countdown ticks on global turn ends and only expires once it reaches the configured duration.
        /// RemovalTiming is forced away from AnyTurn because that branch calls _controller.RemovePassive,
        /// which needs a live controller: deferred removal is the only path reachable in EditMode.
        /// </summary>
        [Test]
        public void OnGlobalTurnEnd_FewerTicksThanDuration_DoesNotExpire()
        {
            _slow.RemovalTiming = PassiveRemovalTiming.OwnerTurnStart;
            _slow.OnEquip(null);
            IOnGlobalTurnEnd ticking = _slow;

            ticking.OnGlobalTurnEnd();
            ticking.OnGlobalTurnEnd();

            Assert.That(_slow.IsExpired, Is.False);
        }

        [Test]
        public void OnGlobalTurnEnd_ReachingTheDuration_MarksItExpired()
        {
            _slow.RemovalTiming = PassiveRemovalTiming.OwnerTurnStart;
            _slow.OnEquip(null);
            IOnGlobalTurnEnd ticking = _slow;

            for (int i = 0; i < 3; i++)
                ticking.OnGlobalTurnEnd();

            Assert.That(_slow.IsExpired, Is.True);
        }

        /// <summary>Reapplying refreshes the duration: that is the point of stacking a slow onto a slow.</summary>
        [Test]
        public void OnReapplied_AfterPartialCountdown_RestartsTheDuration()
        {
            _slow.RemovalTiming = PassiveRemovalTiming.OwnerTurnStart;
            _slow.OnEquip(null);
            IOnGlobalTurnEnd ticking = _slow;
            ticking.OnGlobalTurnEnd();
            ticking.OnGlobalTurnEnd();

            ((IStackablePassive)_slow).OnReapplied(null, _slow);
            ticking.OnGlobalTurnEnd();
            ticking.OnGlobalTurnEnd();

            Assert.That(_slow.IsExpired, Is.False, "Two ticks after the refresh must not reach a duration of 3.");

            ticking.OnGlobalTurnEnd();

            Assert.That(_slow.IsExpired, Is.True);
        }
    }
}
