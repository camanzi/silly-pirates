/// <summary>
/// A phase of a character's life cycle that an animation can be attached to.
/// An extensible enum (e.g. Revive or TurnStart in the future) — used as a dictionary key, so existing
/// values must never be renumbered.
///
/// <see cref="Spawn"/> and <see cref="Leave"/> are transitions: they end on their own and go through
/// <see cref="CharacterLifecycleAnimator.PlayAsync"/>. <see cref="Idle"/> is the first LOOPING phase: it
/// does not end on its own, it runs via <see cref="CharacterLifecycleAnimator.StartLoop"/> until something
/// replaces it. A future looping <c>PostDeath</c> would be added here in exactly the same way.
/// </summary>
public enum LifecyclePhase
{
    Spawn,
    Leave,
    Idle
}
