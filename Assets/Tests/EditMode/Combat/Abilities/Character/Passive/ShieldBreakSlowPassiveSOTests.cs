using NUnit.Framework;

namespace SillyPirates.Tests.EditMode.Combat.Abilities.Character.Passive
{
    /// <summary>
    /// The penalty applied when an elemental shield breaks. Its OnEquip dereferences the controller
    /// (GetComponent&lt;ITurnAgent&gt;) and calls TurnOrderDataSO.SendToBack, so these tests stay on the agility
    /// contract and leave the SendToBack path to TurnOrderDataSOTests, which covers it directly.
    /// </summary>
    public class ShieldBreakSlowPassiveSOTests : ScriptableObjectFixture
    {
        private const float Tolerance = 0.001f;

        private ShieldBreakSlowPassiveSO _shieldBreak;

        [SetUp]
        public void SetUp() => _shieldBreak = NewSO<ShieldBreakSlowPassiveSO>();

        /// <summary>
        /// The invariant the class documents: the punishment IS the drop to the back of the queue, so the
        /// agility penalty defaults to zero. A non-zero default would make HandlePassivesChanged emit an
        /// AVDelta that adds itself to the SendToBack and skews it — the effect would land twice.
        /// </summary>
        [Test]
        public void GetAgilityBonuses_WithDefaultConfiguration_ContributeNothing()
        {
            IAgilityModifier modifier = _shieldBreak;

            Assert.That(modifier.GetFlatAgilityBonus(), Is.EqualTo(0));
            Assert.That(modifier.GetPercentageAgilityBonus(), Is.EqualTo(0f).Within(Tolerance));
        }

        /// <summary>Tuned to a non-zero penalty it behaves as a slow: the contributions come back negated.</summary>
        [Test]
        public void GetAgilityBonuses_WithTunedPenalties_ReturnThemNegated()
        {
            _shieldBreak.FlatPenalty = 15;
            _shieldBreak.PercentPenalty = 30f;

            IAgilityModifier modifier = _shieldBreak;
            Assert.That(modifier.GetFlatAgilityBonus(), Is.EqualTo(-15));
            Assert.That(modifier.GetPercentageAgilityBonus(), Is.EqualTo(-30f).Within(Tolerance));
        }

        /// <summary>
        /// No stacking: it is a separate class from SlowPassiveSO precisely because AddPassive deduplicates by
        /// exact type, and a second SlowPassiveSO would have stacked with an existing one instead.
        /// </summary>
        [Test]
        public void ShieldBreakSlow_IsNotStackable()
        {
            Assert.That(_shieldBreak, Is.Not.InstanceOf<IStackablePassive>());
        }

        /// <summary>
        /// With the shipped duration of 0 the very first global turn end expires it, which is what makes it
        /// disappear on the owner's next turn, in sync with the shield regenerating.
        /// </summary>
        [Test]
        public void OnGlobalTurnEnd_WithZeroDuration_ExpiresOnTheFirstTick()
        {
            _shieldBreak.RemovalTiming = PassiveRemovalTiming.OwnerTurnStart;
            Assert.That(_shieldBreak.DurationInTurns, Is.EqualTo(0), "Precondition: the shipped duration is 0.");

            ((IOnGlobalTurnEnd)_shieldBreak).OnGlobalTurnEnd();

            Assert.That(_shieldBreak.IsExpired, Is.True);
        }

        [Test]
        public void OnGlobalTurnEnd_WithATunedDuration_ExpiresOnlyOnceItIsReached()
        {
            _shieldBreak.RemovalTiming = PassiveRemovalTiming.OwnerTurnStart;
            _shieldBreak.DurationInTurns = 2;
            IOnGlobalTurnEnd ticking = _shieldBreak;

            ticking.OnGlobalTurnEnd();
            Assert.That(_shieldBreak.IsExpired, Is.False);

            ticking.OnGlobalTurnEnd();
            Assert.That(_shieldBreak.IsExpired, Is.True);
        }
    }
}
