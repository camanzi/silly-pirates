using System.Threading;
using PrimeTween;
using UnityEngine;

/// <summary>
/// Il personaggio emerge da sotto il pelo dell'acqua verso la sua posa a riposo, insieme ai suoi satelliti.
/// </summary>
[CreateAssetMenu(menuName = "Combat/Lifecycle Animations/Emerge From Water")]
public class EmergeFromWaterAnimationSO : LifecycleAnimationSO
{
    [SerializeField] private float _depth = 2f;
    [SerializeField] private float _duration = 0.8f;
    [SerializeField] private Ease _ease = Ease.OutQuad;
    [SerializeField] private float _startDelay = 0f;
    [SerializeField] private bool _fadeIn = true; // maschera lo sprite visibile sotto il pelo dell'acqua

    public override void PrepareTarget(in LifecycleAnimationTarget target)
    {
        if (target.Pivot == null) return;

        // La scala torna a riposo perché l'avvio di una fase interrompe i tween altrui (es. lo scale-up di
        // un telegraph), che potrebbero averla lasciata a metà.
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

    public override async Awaitable PlayAsync(LifecycleAnimationContext ctx, CancellationToken token)
    {
        if (_startDelay > 0f)
            await Awaitable.WaitForSecondsAsync(_startDelay, token);

        await PlayAllTargetsAsync(ctx);
    }
}
