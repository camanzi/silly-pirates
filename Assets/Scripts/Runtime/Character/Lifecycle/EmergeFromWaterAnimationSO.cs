using PrimeTween;
using UnityEngine;

/// <summary>
/// The character emerges from below the waterline towards its rest pose, together with its satellites.
/// </summary>
[CreateAssetMenu(menuName = "Combat/Lifecycle Animations/Emerge From Water")]
public class EmergeFromWaterAnimationSO : LifecycleAnimationSO
{
    [Header("Emerge From Water")]
    [SerializeField] private float _depth = 2f;
    [SerializeField] private float _duration = 0.8f;
    [SerializeField] private Ease _ease = Ease.OutQuad;
    [SerializeField] private bool _fadeIn = true; // masks the sprite still visible below the waterline

    public override void PrepareTarget(in LifecycleAnimationTarget target)
    {
        if (target.Pivot == null) return;

        // The scale goes back to rest because starting a phase interrupts other people's tweens (e.g. a
        // telegraph's scale-up), which may have left it halfway through.
        target.Pivot.localPosition = target.RestLocalPosition + Vector3.down * _depth;
        target.Pivot.localScale = target.RestLocalScale;

        if (_fadeIn) target.SetAlpha(0f);
    }

    public override Tween PlayTarget(in LifecycleAnimationTarget target)
    {
        if (target.Pivot == null) return default;

        if (_fadeIn && target.Renderer != null)
            _ = Tween.Alpha(target.Renderer, target.RestColor.a, _duration * 0.5f);

        return Tween.LocalPosition(target.Pivot, target.RestLocalPosition, _duration, _ease);
    }
}
