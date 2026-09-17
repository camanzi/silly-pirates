using PrimeTween;
using UnityEngine;

/// <summary>
/// A single visual target animated by the lifecycle: the body pivot (e.g. <c>MeshHolder</c>) or a
/// "satellite" — a part of a composite enemy, a hat, a weapon — that semantically belongs to the same body
/// but lives OUTSIDE the pivot's hierarchy, and therefore inherits none of its animations.
///
/// The rest pose is captured at registration time and never updated again: it is the reference every
/// animation starts from and every reset returns to.
/// </summary>
public readonly struct LifecycleAnimationTarget
{
    public readonly Transform Pivot;
    public readonly SpriteRenderer Renderer;   // may be null: a satellite with no sprite simply moves
    public readonly Vector3 RestLocalPosition;
    public readonly Vector3 RestLocalScale;
    public readonly Color RestColor;

    public bool IsValid => Pivot != null;

    public LifecycleAnimationTarget(
        Transform pivot,
        SpriteRenderer renderer,
        Vector3 restLocalPosition,
        Vector3 restLocalScale,
        Color restColor)
    {
        Pivot = pivot;
        Renderer = renderer;
        RestLocalPosition = restLocalPosition;
        RestLocalScale = restLocalScale;
        RestColor = restColor;
    }

    /// <summary>Captures <paramref name="pivot"/>'s current pose as its rest pose.</summary>
    public static LifecycleAnimationTarget Capture(Transform pivot, SpriteRenderer renderer)
    {
        Color restColor = renderer != null ? renderer.color : Color.white;
        return new LifecycleAnimationTarget(
            pivot, renderer, pivot.localPosition, pivot.localScale, restColor);
    }

    /// <summary>Restores the transform pose only. Safe on already destroyed objects.</summary>
    public void ResetPoseToRest()
    {
        if (Pivot == null) return;
        Pivot.localPosition = RestLocalPosition;
        Pivot.localScale = RestLocalScale;
    }

    /// <summary>Restores the pose and the full colour (RGBA): used by revives, where the "dead" tint has to go.</summary>
    public void ResetToRest()
    {
        ResetPoseToRest();
        if (Renderer != null) Renderer.color = RestColor;
    }

    /// <summary>
    /// Writes the alpha channel only, preserving the RGB: the tint
    /// <see cref="DirectionalSpriteController.SetDeadVisual"/> applies to a broken part has to survive the
    /// lifecycle fade, or a destroyed part would go back to a living part's colour while it sinks.
    /// </summary>
    public void SetAlpha(float alpha)
    {
        if (Renderer == null) return;
        Color color = Renderer.color;
        color.a = alpha;
        Renderer.color = color;
    }

    /// <summary>
    /// Stops the tweens other systems are already running on this target (a telegraph's infinite shake, a
    /// jump interrupted halfway, an earlier fade). Without this they would keep writing to the same
    /// <c>localPosition</c>/colour the lifecycle is animating.
    /// </summary>
    public void StopActiveTweens()
    {
        if (Pivot != null) Tween.StopAll(Pivot);
        if (Renderer != null) Tween.StopAll(Renderer);
    }
}
