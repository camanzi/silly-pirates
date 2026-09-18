using NUnit.Framework;

namespace SillyPirates.Tests.EditMode.Combat.Pause
{
    /// <summary>
    /// The pause flag shared by the whole combat: the time controller, the audio director, the camera and
    /// the menu all read it, so its two guarantees matter more than they look — a fresh instance is never
    /// paused, and OnPauseChanged fires on a real change only.
    ///
    /// Every test runs on a CreateInstance, never on PauseState.asset: that asset sits in
    /// CombatSession._resettables and a test mutating it would leak a paused game into the Editor.
    /// </summary>
    public class PauseStateSOTests : ScriptableObjectFixture
    {
        private PauseStateSO _state;

        [SetUp]
        public void SetUp() => _state = NewSO<PauseStateSO>();

        [Test]
        public void IsPaused_FreshInstance_IsFalse()
        {
            Assert.That(_state.IsPaused, Is.False);
        }

        [Test]
        public void SetPaused_True_SetsTheFlagAndNotifiesOnce()
        {
            int calls = 0;
            bool last = false;
            _state.OnPauseChanged += value => { calls++; last = value; };

            _state.SetPaused(true);

            Assert.That(_state.IsPaused, Is.True);
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(last, Is.True);
        }

        /// <summary>
        /// A second request for the state already held raises nothing. Escape is not the only way in — the
        /// menu, a transition and ResetForNewCombat can all ask — and every listener reacts by moving
        /// something heavy (Time.timeScale, the audio voices), so a repeated notification is not harmless.
        /// </summary>
        [Test]
        public void SetPaused_SameValueTwice_NotifiesOnlyOnTheChange()
        {
            int calls = 0;
            _state.OnPauseChanged += _ => calls++;

            _state.SetPaused(true);
            _state.SetPaused(true);

            Assert.That(calls, Is.EqualTo(1));
            Assert.That(_state.IsPaused, Is.True);
        }

        [Test]
        public void SetPaused_BackToFalse_NotifiesWithFalse()
        {
            _state.SetPaused(true);

            bool? last = null;
            _state.OnPauseChanged += value => last = value;

            _state.SetPaused(false);

            Assert.That(_state.IsPaused, Is.False);
            Assert.That(last, Is.False);
        }

        /// <summary>
        /// The restart path: the menu raises the transition channel while the game is paused, and
        /// SceneFlowDirector calls ResetForNewCombat between the unload and the load. Were this not to
        /// clear the flag, the freshly loaded combat would come up frozen.
        /// </summary>
        [Test]
        public void ResetForNewCombat_WhilePaused_ClearsTheFlagAndNotifies()
        {
            _state.SetPaused(true);

            bool? last = null;
            _state.OnPauseChanged += value => last = value;

            _state.ResetForNewCombat();

            Assert.That(_state.IsPaused, Is.False);
            Assert.That(last, Is.False);
        }

        [Test]
        public void ResetForNewCombat_WhenNotPaused_NotifiesNothing()
        {
            int calls = 0;
            _state.OnPauseChanged += _ => calls++;

            _state.ResetForNewCombat();

            Assert.That(calls, Is.EqualTo(0));
            Assert.That(_state.IsPaused, Is.False);
        }
    }
}
