using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The context handed to the <see cref="LifecycleAnimationSO"/> assets so they can stay stateless:
/// all mutable state lives here, not on the SO.
/// </summary>
public readonly struct LifecycleAnimationContext
{
    private static readonly LifecycleAnimationTarget[] NoTargets = new LifecycleAnimationTarget[0];

    public readonly Transform Root;   // the character's transform (grid position) — do NOT touch

    private readonly IReadOnlyList<LifecycleAnimationTarget> _targets;

    /// <summary>
    /// The targets to animate: <c>[0]</c> is always the body, followed by the registered satellites.
    /// The list is LIVE — it is the very instance owned by the <see cref="CharacterLifecycleAnimator"/> —
    /// so a satellite registered after the context was built (the ordering between <c>Awake</c> calls is
    /// not guaranteed) shows up here without the context having to be rebuilt.
    /// </summary>
    public IReadOnlyList<LifecycleAnimationTarget> Targets => _targets ?? NoTargets;

    public LifecycleAnimationTarget Body => Targets.Count > 0 ? Targets[0] : default;

    // Shorthands onto the body: they keep the syntax unchanged for the SOs and callers written back when
    // the context had a single target.
    public Transform AnimationRoot => Body.Pivot;
    public SpriteRenderer Renderer => Body.Renderer;
    public Vector3 RestLocalPosition => Body.RestLocalPosition;
    public Vector3 RestLocalScale => Body.RestLocalScale;
    public Color RestColor => Body.RestColor;

    public LifecycleAnimationContext(Transform root, IReadOnlyList<LifecycleAnimationTarget> targets)
    {
        Root = root;
        _targets = targets;
    }
}
