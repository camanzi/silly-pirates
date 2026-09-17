using System;
using UnityEngine;

/// <summary>Where the phase's VFX is anchored in the world.</summary>
public enum LifecycleVfxAnchor
{
    /// <summary>The character's grid transform: still during the animation, it is the waterline.</summary>
    CharacterRoot,

    /// <summary>The body's visual pivot, sampled once at Raise time (a fixed effect).</summary>
    BodyPivot,

    /// <summary>Follows the body's visual pivot for the whole duration of the effect.</summary>
    FollowBody
}

/// <summary>
/// An inner moment of a <see cref="LifecycleAnimationSO"/> that a list of VFX is attached to.
/// Used as a dictionary key, so existing values must never be renumbered
/// (the same constraint as <see cref="LifecyclePhase"/>).
/// </summary>
public enum LifecycleVfxStage
{
    /// <summary>
    /// Right after the phase's initial state has been applied (e.g. the sprite already underwater), before
    /// the movement begins. It is the only stage also reached by
    /// <see cref="CharacterLifecycleAnimator.PrepareHidden"/>: during the combat intro it starts here and
    /// stays on until the director closes the phase.
    /// </summary>
    Prepare,

    /// <summary>In sync with the tweens starting.</summary>
    Movement,

    /// <summary>Once the movement is over.</summary>
    End
}

/// <summary>
/// An inline descriptor of a VFX attached to a stage of a <see cref="LifecycleAnimationSO"/>.
/// <c>[Serializable]</c> and not a separate ScriptableObject: the configuration is meant to be edited in
/// the same Inspector as the animation it belongs to, with no satellite asset to create and wire up.
/// </summary>
[Serializable]
public class LifecycleVfxSpec
{
    [SerializeField] private VFXController _prefab;
    [SerializeField] private LifecycleVfxAnchor _anchor = LifecycleVfxAnchor.CharacterRoot;

    [Tooltip("A persistent effect: it stays on until the phase ends, instead of running out on its own.")]
    [SerializeField] private bool _loop;

    [Tooltip("Applies only to the fixed anchors (CharacterRoot/BodyPivot): with FollowBody the VfxDirector " +
             "overwrites the position every LateUpdate (VfxDirector.cs), so the offset would be lost.")]
    [SerializeField] private Vector3 _offset;

    [SerializeField] private float _scale = 1f;

    public bool IsConfigured => _prefab != null;

    public bool Loop => _loop;

    /// <summary>
    /// Resolves the anchor, raises the right cue on the channel and returns the minted handle (only for
    /// specs in <see cref="Loop"/>; <see cref="VfxHandle.None"/> for one-shots).
    /// A defensive no-op when the channel is missing or the spec is not configured.
    /// </summary>
    public VfxHandle Raise(VfxCueEventChannel channel, in LifecycleAnimationContext ctx)
    {
        if (channel == null || !IsConfigured) return VfxHandle.None;

        VfxHandle handle = _loop ? VfxHandle.New() : VfxHandle.None;
        channel.RaiseEvent(BuildCue(ctx, handle));
        return handle;
    }

    private VfxCue BuildCue(in LifecycleAnimationContext ctx, VfxHandle handle)
    {
        // Defensive fallback: a satellite or body with no valid pivot must not crash the Raise, it
        // anchors to the grid transform as if it were CharacterRoot.
        Transform bodyPivot = ctx.Body.Pivot != null ? ctx.Body.Pivot : ctx.Root;

        switch (_anchor)
        {
            case LifecycleVfxAnchor.FollowBody:
                return handle.IsValid
                    ? VfxCue.Persistent(_prefab, bodyPivot, handle, _scale)
                    : VfxCue.Follow(_prefab, bodyPivot, _scale);

            case LifecycleVfxAnchor.BodyPivot:
                return handle.IsValid
                    ? VfxCue.PersistentAt(_prefab, bodyPivot.position + _offset, handle, _scale)
                    : VfxCue.At(_prefab, bodyPivot.position + _offset, _scale);

            case LifecycleVfxAnchor.CharacterRoot:
            default:
                Vector3 position = ctx.Root.position + _offset;
                return handle.IsValid
                    ? VfxCue.PersistentAt(_prefab, position, handle, _scale)
                    : VfxCue.At(_prefab, position, _scale);
        }
    }
}
