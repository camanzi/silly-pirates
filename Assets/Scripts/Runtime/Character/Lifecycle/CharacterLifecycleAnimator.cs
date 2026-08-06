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

    /// <summary>
    /// Pivot visivo (es. MeshHolder) per animazioni una-tantum esterne alle <see cref="LifecyclePhase"/>,
    /// come i salti in combattimento. Se il prefab non ha un pivot dedicato, <see cref="EnsureRestPoseCaptured"/>
    /// ha già fatto fallback su <c>transform</c> loggando un errore: i chiamanti devono trattare
    /// "<c>AnimationRoot == transform</c>" come "nessun pivot sicuro disponibile" e NON animarlo,
    /// per non spostare collider e posizione di griglia.
    /// </summary>
    public Transform AnimationRoot
    {
        get
        {
            EnsureRestPoseCaptured();
            return _animationRoot;
        }
    }

    private LifecycleAnimationContext _context;
    private bool _restPoseCaptured;

    private void Awake() => EnsureRestPoseCaptured();

    /// <summary>
    /// Cattura una sola volta la posa a riposo, risolvendo prima i riferimenti mancanti.
    /// Lazy e idempotente perché su <c>Object.Instantiate</c> l'<c>OnEnable</c> di un altro componente
    /// (<see cref="HostileCharacter.OnCombatJoin"/> → <see cref="Play"/>) può girare prima di questo
    /// <c>Awake</c>: senza la cattura lazy il contesto resterebbe <c>default</c> e l'animazione
    /// verrebbe saltata in silenzio. Il flag garantisce che la posa non venga mai ricatturata dopo
    /// che un'animazione l'ha spostata — requisito di <see cref="ResetToRest"/>.
    /// </summary>
    private void EnsureRestPoseCaptured()
    {
        if (_restPoseCaptured) return;
        _restPoseCaptured = true;

        if (_spriteRenderer == null) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (_animationRoot == null)
        {
            _animationRoot = transform;
            Debug.LogError(
                $"[{nameof(CharacterLifecycleAnimator)}] '{name}': _animationRoot non assegnato. " +
                "Le animazioni sposteranno il transform di griglia (collider inclusi) invece del solo " +
                "pivot visivo. Assegnare un child pivot (es. MeshHolder).", this);
        }

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

    /// <summary>
    /// Applica istantaneamente lo stato "nascosto" di una fase (es. pivot sott'acqua, alpha 0) senza
    /// riprodurre l'animazione. Usato dalla sequenza di intro al combattimento per sopprimere lo
    /// spawn automatico di <see cref="HostileCharacter.OnCombatJoin"/> finché il regista non decide
    /// di rigiocarlo con <see cref="PlayAsync"/>. Stessa risoluzione della SO di <see cref="PlayAsync"/>,
    /// stessi messaggi di log: la SO resta stateless, tutto lo stato mutabile vive nel contesto.
    /// </summary>
    public void PrepareHidden(LifecyclePhase phase)
    {
        EnsureRestPoseCaptured();

        if (_animations == null || _animations.Count == 0)
        {
            Debug.LogWarning(
                $"[{nameof(CharacterLifecycleAnimator)}] '{name}': dizionario animazioni vuoto, " +
                $"fase {phase} ignorata.", this);
            return;
        }

        if (!_animations.TryGetValue(phase, out LifecycleAnimationSO animation))
            return;

        if (animation == null)
        {
            Debug.LogWarning(
                $"[{nameof(CharacterLifecycleAnimator)}] '{name}': la fase {phase} è nel dizionario " +
                "ma non ha una LifecycleAnimationSO assegnata.", this);
            return;
        }

        animation.Prepare(in _context);
    }

    public async Awaitable PlayAsync(LifecyclePhase phase, CancellationToken token)
    {
        EnsureRestPoseCaptured();

        // Dizionario vuoto: non è mai una configurazione voluta — un prefab senza animazioni di
        // lifecycle non ha nemmeno questo componente. In build indica che i dati serializzati sono
        // andati persi sul clone (vedi SerializedDictionary.OnAfterDeserialize, che in player
        // svuota la lista di backing dopo la prima deserializzazione).
        if (_animations == null || _animations.Count == 0)
        {
            Debug.LogWarning(
                $"[{nameof(CharacterLifecycleAnimator)}] '{name}': dizionario animazioni vuoto, " +
                $"fase {phase} ignorata.", this);
            return;
        }

        // Fase non configurata: silenzioso, è la configurazione voluta (non tutti i personaggi
        // hanno un'animazione per ogni fase).
        if (!_animations.TryGetValue(phase, out LifecycleAnimationSO animation))
            return;

        // Entry presente ma senza SO: quasi certamente un wiring dimenticato, non un'assenza voluta.
        if (animation == null)
        {
            Debug.LogWarning(
                $"[{nameof(CharacterLifecycleAnimator)}] '{name}': la fase {phase} è nel dizionario " +
                "ma non ha una LifecycleAnimationSO assegnata.", this);
            return;
        }

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
        EnsureRestPoseCaptured();

        if (_animationRoot != null)
        {
            _animationRoot.localPosition = _context.RestLocalPosition;
            _animationRoot.localScale = _context.RestLocalScale;
        }

        if (_spriteRenderer != null)
            _spriteRenderer.color = _context.RestColor;
    }
}
