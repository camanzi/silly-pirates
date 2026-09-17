using PrimeTween;
using UnityEngine;

/// <summary>
/// The character sinks below the waterline starting from its rest pose, together with its satellites.
/// The inverse of <see cref="EmergeFromWaterAnimationSO"/>.
/// </summary>
[CreateAssetMenu(menuName = "Combat/Lifecycle Animations/Sink Under Water")]
public class SinkUnderWaterAnimationSO : LifecycleAnimationSO
{
    [Header("Sink Under Water")]
    [SerializeField] private float _depth = 2f;
    [SerializeField] private float _duration = 0.8f;
    [SerializeField] private Ease _ease = Ease.OutQuad;
    [SerializeField] private bool _fadeOut = true;

    public override void PrepareTarget(in LifecycleAnimationTarget target)
    {
        if (target.Pivot == null) return;

        // Defensive: it returns to the rest pose in case an earlier animation (or an ability tween just
        // interrupted) left the pivot or the scale in an intermediate state.
        target.Pivot.localPosition = target.RestLocalPosition;
        target.Pivot.localScale = target.RestLocalScale;

        // Alpha only: the "dead" tint of something already destroyed has to stay visible while it sinks.
        target.SetAlpha(target.RestColor.a);
    }

    public override Tween PlayTarget(in LifecycleAnimationTarget target)
    {
        if (target.Pivot == null) return default;

        if (_fadeOut && target.Renderer != null)
            _ = Tween.Alpha(target.Renderer, 0f, _duration * 0.5f, startDelay: _duration * 0.5f);

        return Tween.LocalPosition(
            target.Pivot, target.RestLocalPosition + Vector3.down * _depth, _duration, _ease);
    }
}
