using System.Threading;
using PrimeTween;
using UnityEngine;

/// <summary>
/// Il personaggio affonda sotto il pelo dell'acqua a partire dalla sua posa a riposo. Inverso di
/// <see cref="EmergeFromWaterAnimationSO"/>.
/// </summary>
[CreateAssetMenu(menuName = "Combat/Lifecycle Animations/Sink Under Water")]
public class SinkUnderWaterAnimationSO : LifecycleAnimationSO
{
    [SerializeField] private float _depth = 2f;
    [SerializeField] private float _duration = 0.8f;
    [SerializeField] private Ease _ease = Ease.OutQuad;
    [SerializeField] private float _startDelay = 0f;
    [SerializeField] private bool _fadeOut = true;

    public override void Prepare(in LifecycleAnimationContext ctx)
    {
        // Difensivo: riporta alla posa a riposo nel caso una precedente animazione fosse
        // stata interrotta lasciando pivot/colore in uno stato intermedio.
        if (ctx.AnimationRoot != null)
            ctx.AnimationRoot.localPosition = ctx.RestLocalPosition;

        if (ctx.Renderer != null)
            ctx.Renderer.color = ctx.RestColor;
    }

    public override async Awaitable PlayAsync(LifecycleAnimationContext ctx, CancellationToken token)
    {
        if (_startDelay > 0f)
            await Awaitable.WaitForSecondsAsync(_startDelay, token);

        Vector3 sunkPosition = ctx.RestLocalPosition + Vector3.down * _depth;

        if (_fadeOut && ctx.Renderer != null)
            _ = Tween.Alpha(ctx.Renderer, 0f, _duration * 0.5f, startDelay: _duration * 0.5f);

        await Tween.LocalPosition(ctx.AnimationRoot, sunkPosition, _duration, _ease);
    }
}
