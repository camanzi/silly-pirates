using PrimeTween;
using UnityEngine;

/// <summary>
/// The enemies' baseline idle: a slow oscillation on the Y axis, position only — never the scale, which
/// the loop must not claim (<see cref="JumpSquashStretchHelper"/> and the legacy squash-stretch commands
/// are already using it). The reference implementation to copy for the next loops (e.g. a future
/// PostDeath).
/// </summary>
[CreateAssetMenu(menuName = "Combat/Lifecycle Animations/Base Enemy Idle")]
public class BaseEnemyIdleAnimationSO : LoopingLifecycleAnimationSO
{
    [Header("Base Enemy Idle")]
    [SerializeField] private float _amplitude = 0.12f;
    [SerializeField] private float _duration = 1.6f;
    [SerializeField] private Ease _ease = Ease.InOutSine;

    [Tooltip("Random initial phase offset, in seconds: without it the body and satellites of a composite " +
             "enemy (e.g. BigEvilCube) would sway as one block instead of as independent parts.")]
    [SerializeField] private float _maxStartOffset = 0.8f;

    public override void PrepareTarget(in LifecycleAnimationTarget target)
    {
        if (target.Pivot == null) return;

        // Position only: starting a loop must not touch a scale another animation may have left halfway
        // through, restoring it is not this one's job.
        target.Pivot.localPosition = target.RestLocalPosition;
    }

    public override Tween PlayTarget(in LifecycleAnimationTarget target)
    {
        if (target.Pivot == null) return default;

        // Random.Range is read NOW, at call time, and never stored: the SO stays stateless and shared
        // between prefabs, and the offset is per-target-per-start, not per-asset.
        return Tween.LocalPositionY(
            target.Pivot, target.RestLocalPosition.y + _amplitude, _duration, _ease,
            cycles: -1, cycleMode: CycleMode.Yoyo,
            startDelay: Random.Range(0f, _maxStartOffset));
    }
}
