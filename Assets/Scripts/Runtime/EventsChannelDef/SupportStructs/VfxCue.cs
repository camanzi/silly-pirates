using UnityEngine;

/// <summary>
/// A request to play a particle effect. Fire-and-forget like <see cref="SfxCue"/>: nobody waits for it
/// to end, and the VfxDirector returns the instance to the pool on its own.
///
/// The one exception is the persistent VFX, which carries a valid <see cref="VfxHandle"/> and has to be
/// stopped explicitly on the stop channel.
/// </summary>
public struct VfxCue
{
    public VFXController Prefab;

    /// <summary>Position in the world. Ignored when FollowTarget is set.</summary>
    public Vector3 Position;

    public Quaternion Rotation;

    /// <summary>Uniform scale applied to the instance. 0 is read as 1.</summary>
    public float Scale;

    /// <summary>When set, the instance follows this Transform instead of standing still.</summary>
    public Transform FollowTarget;

    /// <summary>Valid = a persistent effect: no automatic release, it is stopped through the stop channel.</summary>
    public VfxHandle Handle;

    /// <summary>A one-shot effect at a fixed point: no per-frame cost.</summary>
    public static VfxCue At(VFXController prefab, Vector3 position, float scale = 1f) => new()
    {
        Prefab = prefab,
        Position = position,
        Rotation = Quaternion.identity,
        Scale = scale,
        FollowTarget = null,
        Handle = VfxHandle.None
    };

    /// <summary>An oriented one-shot effect, for muzzle flashes and directional impacts.</summary>
    public static VfxCue At(VFXController prefab, Vector3 position, Quaternion rotation, float scale = 1f) => new()
    {
        Prefab = prefab,
        Position = position,
        Rotation = rotation,
        Scale = scale,
        FollowTarget = null,
        Handle = VfxHandle.None
    };

    /// <summary>A one-shot effect that follows a moving target.</summary>
    public static VfxCue Follow(VFXController prefab, Transform target, float scale = 1f) => new()
    {
        Prefab = prefab,
        Position = target != null ? target.position : Vector3.zero,
        Rotation = Quaternion.identity,
        Scale = scale,
        FollowTarget = target,
        Handle = VfxHandle.None
    };

    /// <summary>
    /// An effect that stays on until it is stopped through its handle. It follows the target instead of
    /// being parented to it: a pooled object must never be reparented onto a gameplay object.
    /// </summary>
    public static VfxCue Persistent(VFXController prefab, Transform target, VfxHandle handle, float scale = 1f) => new()
    {
        Prefab = prefab,
        Position = target != null ? target.position : Vector3.zero,
        Rotation = Quaternion.identity,
        Scale = scale,
        FollowTarget = target,
        Handle = handle
    };

    /// <summary>
    /// An effect that stays on until it is stopped through its handle, but pinned to a world point
    /// instead of following a Transform (e.g. foam on the waterline: the emergence point does not move).
    /// It also avoids the per-frame cost in VfxDirector.LateUpdate that Persistent() would pay for an
    /// effect that has no need of it.
    /// </summary>
    public static VfxCue PersistentAt(VFXController prefab, Vector3 position, VfxHandle handle, float scale = 1f) => new()
    {
        Prefab = prefab,
        Position = position,
        Rotation = Quaternion.identity,
        Scale = scale,
        FollowTarget = null,
        Handle = handle
    };
}
