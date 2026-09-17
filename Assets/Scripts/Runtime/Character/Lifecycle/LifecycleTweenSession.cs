using System.Collections.Generic;
using PrimeTween;

/// <summary>
/// The handles of the PrimeTween tweens opened by the lifecycle loop track (see
/// <see cref="LoopingLifecycleAnimationSO"/>). The twin of <see cref="LifecycleVfxSession"/> but for
/// tweens rather than VFX, and for the same reason: it lives on the
/// <see cref="CharacterLifecycleAnimator"/> and NOT on the SO, which stays shared between different
/// prefabs and therefore cannot own per-instance handles — two enemies using the same idle asset would
/// fight over a single field.
///
/// It is plural because a composite target (body + satellites) starts N tweens that have to be stopped
/// together; a standard enemy's body is simply the N=1 case.
///
/// A single instance reused loop after loop: no allocation per repeated start.
/// </summary>
public sealed class LifecycleTweenSession
{
    private readonly List<Tween> _open = new();

    /// <summary>The loop is currently registered as running. Mirrors <see cref="LifecycleVfxSession.IsOpen"/>.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>
    /// Opens the session for a new loop. Defensive like <see cref="LifecycleVfxSession.Begin"/>: it stops
    /// the previous one, so a loop can never outlive the one that replaced it.
    /// </summary>
    public void Begin()
    {
        StopAll();
        IsOpen = true;
    }

    /// <summary>Registers a tween to be stopped by <see cref="StopAll"/>. Already dead tweens (zero duration, destroyed target) are ignored.</summary>
    public void Track(Tween tween)
    {
        if (tween.isAlive) _open.Add(tween);
    }

    /// <summary>
    /// Stops every open tween and closes the session. Idempotent: a <c>Stop()</c> on an already finished
    /// tween is a no-op on PrimeTween's side.
    /// </summary>
    public void StopAll()
    {
        for (int i = 0; i < _open.Count; i++)
            if (_open[i].isAlive) _open[i].Stop();

        _open.Clear();
        IsOpen = false;
    }
}
