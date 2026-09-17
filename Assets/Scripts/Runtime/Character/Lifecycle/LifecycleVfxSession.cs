using System.Collections.Generic;

/// <summary>
/// The handles of the persistent VFX opened during a lifecycle phase, together with the channel they have
/// to be stopped on.
///
/// It lives on the <see cref="CharacterLifecycleAnimator"/> and NOT on the <see cref="LifecycleAnimationSO"/>:
/// that is an asset shared between different prefabs, so a field there would be overwritten if two
/// characters ran the same phase in the same frame. The animator, by contrast, is per-instance, and it is
/// also the only one that knows when the phase truly ends — including the phase prepared by
/// <see cref="CharacterLifecycleAnimator.PrepareHidden"/> and never played, which no <c>finally</c> inside
/// the SO could ever catch.
///
/// A single instance reused phase after phase: no allocation per repeated spawn.
/// </summary>
public sealed class LifecycleVfxSession
{
    private readonly List<VfxHandle> _open = new();
    private VfxStopEventChannel _stopChannel;

    /// <summary>A phase has already raised its <see cref="LifecycleVfxStage.Prepare"/> VFX.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>
    /// Opens the session for a new phase. Defensive: it closes the previous one if it was left open, so a
    /// loop can never outlive the phase that started it.
    /// </summary>
    public void Begin(VfxStopEventChannel stopChannel)
    {
        StopAll();

        _stopChannel = stopChannel;
        IsOpen = true;
    }

    /// <summary>Registers a handle to be stopped when the phase ends. One-shot handles are invalid and are ignored.</summary>
    public void Track(VfxHandle handle)
    {
        if (!handle.IsValid) return;
        _open.Add(handle);
    }

    /// <summary>
    /// Stops every open persistent effect and closes the session. Idempotent: a second stop on the same
    /// handle is a no-op miss on <see cref="VfxDirector"/>'s side.
    /// </summary>
    public void StopAll()
    {
        if (_stopChannel != null)
        {
            for (int i = 0; i < _open.Count; i++)
                _stopChannel.RaiseEvent(_open[i]);
        }

        _open.Clear();
        _stopChannel = null;
        IsOpen = false;
    }
}
