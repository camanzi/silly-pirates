using System;
using System.Threading;
using AYellowpaper.SerializedCollections;
using UnityEngine;

/// <summary>
/// Riproduce animazioni di lifecycle (spawn, morte, ecc.) su un personaggio, agnostico rispetto
/// a <see cref="HostileCharacter"/> / <see cref="GridCharacter"/>. Le <see cref="LifecycleAnimationSO"/>
/// referenziate sono stateless e condivise: nessun Instantiate per-istanza.
/// </summary>
public class CharacterLifecycleAnimator : MonoBehaviour
{
    [SerializeField] private Transform _animationRoot;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private SerializedDictionary<LifecyclePhase, LifecycleAnimationSO> _animations;

    public bool IsPlaying { get; private set; }

    private LifecycleAnimationContext _context;

    private void Awake()
    {
        if (_animationRoot == null) _animationRoot = transform;
        if (_spriteRenderer == null) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        CaptureRestPose();
    }

    private void CaptureRestPose()
    {
        Color restColor = _spriteRenderer != null ? _spriteRenderer.color : Color.white;
        _context = new LifecycleAnimationContext(
            transform,
            _animationRoot,
            _spriteRenderer,
            _animationRoot.localPosition,
            _animationRoot.localScale,
            restColor);
    }

    /// <summary>Fire-and-forget: avvia l'animazione senza attendere il completamento.</summary>
    public async void Play(LifecyclePhase phase)
    {
        try
        {
            await PlayAsync(phase, destroyCancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Distrutto durante l'animazione: nulla da fare.
        }
    }

    public async Awaitable PlayAsync(LifecyclePhase phase, CancellationToken token)
    {
        if (_animations == null || !_animations.TryGetValue(phase, out LifecycleAnimationSO animation) || animation == null)
            return;

        // Guardia: se Awake non ha ancora catturato la posa (ordine di inizializzazione dei componenti)
        // non c'è nulla su cui animare — meglio saltare che far esplodere un tween su un target null.
        if (_context.AnimationRoot == null) return;

        animation.Prepare(in _context);

        IsPlaying = true;
        try
        {
            await animation.PlayAsync(_context, token);
        }
        finally
        {
            IsPlaying = false;
        }
    }

    /// <summary>
    /// Spin-wait su <see cref="IsPlaying"/>: necessario perché l'Awaitable di Unity è single-consumption
    /// e non può essere salvato in un campo da <see cref="Play"/> per essere atteso altrove
    /// (stesso pattern di <c>EnemyTurnDriver.ExecuteTurnAsync</c>).
    /// </summary>
    public async Awaitable WaitUntilIdleAsync(CancellationToken token)
    {
        while (IsPlaying)
        {
            token.ThrowIfCancellationRequested();
            await Awaitable.NextFrameAsync(token);
        }
    }

    public void ResetToRest()
    {
        if (_animationRoot != null)
        {
            _animationRoot.localPosition = _context.RestLocalPosition;
            _animationRoot.localScale = _context.RestLocalScale;
        }

        if (_spriteRenderer != null)
            _spriteRenderer.color = _context.RestColor;
    }
}
