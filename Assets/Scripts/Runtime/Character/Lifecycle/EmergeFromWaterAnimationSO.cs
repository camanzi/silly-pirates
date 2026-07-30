using System.Threading;
using PrimeTween;
using UnityEngine;

/// <summary>
/// Il personaggio emerge da sotto il pelo dell'acqua verso la sua posa a riposo.
/// </summary>
[CreateAssetMenu(menuName = "Combat/Lifecycle Animations/Emerge From Water")]
public class EmergeFromWaterAnimationSO : LifecycleAnimationSO
{
    [SerializeField] private float _depth = 2f;
    [SerializeField] private float _duration = 0.8f;
    [SerializeField] private Ease _ease = Ease.OutQuad;
    [SerializeField] private float _startDelay = 0f;
    [SerializeField] private bool _fadeIn = true; // maschera lo sprite visibile sotto il pelo dell'acqua

    public override void Prepare(in LifecycleAnimationContext ctx)
    {
        if (ctx.AnimationRoot != null)
            ctx.AnimationRoot.localPosition = ctx.RestLocalPosition + Vector3.down * _depth;

        if (_fadeIn && ctx.Renderer != null)
        {
            Color startColor = ctx.RestColor;
            startColor.a = 0f;
            ctx.Renderer.color = startColor;
        }
    }

    public override async Awaitable PlayAsync(LifecycleAnimationContext ctx, CancellationToken token)
    {
        if (_startDelay > 0f)
            await Awaitable.WaitForSecondsAsync(_startDelay, token);

        if (_fadeIn && ctx.Renderer != null)
            _ = Tween.Alpha(ctx.Renderer, ctx.RestColor.a, _duration * 0.5f);

        await Tween.LocalPosition(ctx.AnimationRoot, ctx.RestLocalPosition, _duration, _ease);
    }
}
