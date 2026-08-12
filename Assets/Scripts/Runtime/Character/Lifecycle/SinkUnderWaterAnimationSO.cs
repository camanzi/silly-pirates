using System.Threading;
using PrimeTween;
using UnityEngine;

/// <summary>
/// Il personaggio affonda sotto il pelo dell'acqua a partire dalla sua posa a riposo, insieme ai suoi
/// satelliti. Inverso di <see cref="EmergeFromWaterAnimationSO"/>.
/// </summary>
[CreateAssetMenu(menuName = "Combat/Lifecycle Animations/Sink Under Water")]
public class SinkUnderWaterAnimationSO : LifecycleAnimationSO
{
    [SerializeField] private float _depth = 2f;
    [SerializeField] private float _duration = 0.8f;
    [SerializeField] private Ease _ease = Ease.OutQuad;
    [SerializeField] private float _startDelay = 0f;
    [SerializeField] private bool _fadeOut = true;

    public override void PrepareTarget(in LifecycleAnimationTarget target)
    {
        if (target.Pivot == null) return;

        // Difensivo: riporta alla posa a riposo nel caso un'animazione precedente (o un tween di abilità
        // appena interrotto) avesse lasciato pivot o scala in uno stato intermedio.
        target.Pivot.localPosition = target.RestLocalPosition;
        target.Pivot.localScale = target.RestLocalScale;

        // Solo l'alpha: il tint "morto" di chi è già stato distrutto deve restare visibile mentre affonda.
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

    public override async Awaitable PlayAsync(LifecycleAnimationContext ctx, CancellationToken token)
    {
        if (_startDelay > 0f)
            await Awaitable.WaitForSecondsAsync(_startDelay, token);

        await PlayAllTargetsAsync(ctx);
    }
}
