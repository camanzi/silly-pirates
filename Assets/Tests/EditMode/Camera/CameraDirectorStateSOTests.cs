using NUnit.Framework;

namespace SillyPirates.Tests.EditMode.Camera
{
    /// <summary>
    /// IsCinematicActive is what tells the free-roam camera target to stop reading input, so a stale true
    /// leaves the player unable to pan and a premature false lets the target be dragged away behind a shot
    /// nobody is looking through. The cases below are the ones the call sites actually produce: nested
    /// BeginFocus with a single closing EndFocus (a multi-beat command, the intro), an unpaired EndFocus
    /// (EnemyTurnDriver's finally runs whether or not a cue was raised, and TurnController releases again
    /// after the action loop), and SignalFocusReady, which means "shot composed", not "cinematic over".
    ///
    /// Every test runs on a CreateInstance, never on CameraDirectorState.asset: that asset sits in
    /// CombatSession._resettables and a test mutating it would leak into the Editor.
    /// </summary>
    public class CameraDirectorStateSOTests : ScriptableObjectFixture
    {
        private CameraDirectorStateSO _state;

        [SetUp]
        public void SetUp() => _state = NewSO<CameraDirectorStateSO>();

        [Test]
        public void IsCinematicActive_FreshInstance_IsFalse()
        {
            Assert.That(_state.IsCinematicActive, Is.False);
        }

        [Test]
        public void BeginFocus_SetsTheFlag()
        {
            _state.BeginFocus();

            Assert.That(_state.IsCinematicActive, Is.True);
            Assert.That(_state.IsFocused, Is.False);
        }

        /// <summary>
        /// The director signals ready as soon as the Brain blend has settled, while the command it is framing
        /// is still running. Were this to clear the flag, the input would come back mid-shot — exactly the bug
        /// the flag exists to prevent.
        /// </summary>
        [Test]
        public void SignalFocusReady_DoesNotEndTheCinematic()
        {
            _state.BeginFocus();

            _state.SignalFocusReady();

            Assert.That(_state.IsFocused, Is.True);
            Assert.That(_state.IsCinematicActive, Is.True);
        }

        [Test]
        public void EndFocus_ClearsTheFlagAndNotifies()
        {
            _state.BeginFocus();

            int calls = 0;
            _state.OnFocusEnded += () => calls++;

            _state.EndFocus();

            Assert.That(_state.IsCinematicActive, Is.False);
            Assert.That(_state.IsFocused, Is.True);
            Assert.That(calls, Is.EqualTo(1));
        }

        /// <summary>
        /// RaiseCueAndWaitAsync begins a focus per beat and never ends one; the single EndFocus comes from the
        /// owner's finally. A counter would end up out of step here — the flag must not.
        /// </summary>
        [Test]
        public void NestedBeginFocus_ThenOneEndFocus_ClearsTheFlag()
        {
            _state.BeginFocus();
            _state.BeginFocus();
            _state.BeginFocus();

            _state.EndFocus();

            Assert.That(_state.IsCinematicActive, Is.False);
        }

        [Test]
        public void EndFocus_WithNoBeginFocus_LeavesTheFlagClear()
        {
            int calls = 0;
            _state.OnFocusEnded += () => calls++;

            _state.EndFocus();

            Assert.That(_state.IsCinematicActive, Is.False);
            Assert.That(_state.IsFocused, Is.True);
            Assert.That(calls, Is.EqualTo(1));
        }

        /// <summary>
        /// A cue interrupted by a scene unload: without the reset the next combat would start with the pan
        /// input locked and no cue left to release it.
        /// </summary>
        [Test]
        public void ResetForNewCombat_MidCinematic_ClearsTheFlag()
        {
            _state.BeginFocus();

            _state.ResetForNewCombat();

            Assert.That(_state.IsCinematicActive, Is.False);
            Assert.That(_state.IsFocused, Is.True);
        }
    }
}
