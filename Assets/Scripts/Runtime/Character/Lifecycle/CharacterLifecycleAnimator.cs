using System;
using System.Collections.Generic;
using System.Threading;
using AYellowpaper.SerializedCollections;
using UnityEngine;

/// <summary>
/// Riproduce animazioni di lifecycle (spawn, morte, ecc.) su un personaggio, agnostico rispetto
/// a <see cref="HostileCharacter"/> / <see cref="GridCharacter"/>. Le <see cref="LifecycleAnimationSO"/>
/// referenziate sono stateless e condivise: nessun Instantiate per-istanza.
///
/// Oltre al pivot del corpo l'animator gestisce dei "satelliti" registrati da terzi
/// (<see cref="RegisterSatellite"/>): oggetti che appartengono visivamente al personaggio ma vivono fuori
/// dalla gerarchia del pivot — le parti di un nemico composito, per esempio — e che quindi non
/// erediterebbero nulla dalle animazioni del corpo.
/// </summary>
public class CharacterLifecycleAnimator : MonoBehaviour
{
    [SerializeField] private Transform _animationRoot;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private SerializedDictionary<LifecyclePhase, LifecycleAnimationSO> _animations;

    public bool IsPlaying { get; private set; }

    /// <summary>Sollevato dopo il <c>Prepare</c> di una fase, sia in <see cref="PrepareHidden"/> sia in <see cref="PlayAsync"/>.</summary>
    public event Action<LifecyclePhase> OnPhaseStarted;

    /// <summary>Sollevato solo al completamento effettivo (non su cancellazione).</summary>
    public event Action<LifecyclePhase> OnPhaseCompleted;

    /// <summary>Fase preparata o in corso; <c>null</c> quando il personaggio è a riposo.</summary>
    public LifecyclePhase? ActivePhase => _activeAnimation != null ? _activePhase : null;

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

    /// <summary>
    /// Tutti i bersagli visivi: <c>[0]</c> è il corpo, gli altri i satelliti registrati. Esposta perché
    /// anche le animazioni fuori dalle <see cref="LifecyclePhase"/> (i salti) devono muovere il corpo
    /// INSIEME ai satelliti, altrimenti le parti resterebbero a terra mentre il corpo salta.
    /// </summary>
    public IReadOnlyList<LifecycleAnimationTarget> Targets
    {
        get
        {
            EnsureRestPoseCaptured();
            return _targets;
        }
    }

    private readonly List<LifecycleAnimationTarget> _targets = new();
    private LifecycleAnimationContext _context;
    private bool _restPoseCaptured;

    // Non-null solo fra l'inizio di una fase e il suo completamento: serve ad agganciare alla fase in
    // corso un satellite che si registra in ritardo.
    private LifecycleAnimationSO _activeAnimation;
    private LifecyclePhase _activePhase;

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

        // Il corpo occupa sempre l'indice 0; i satelliti si accodano dopo.
        _targets.Add(LifecycleAnimationTarget.Capture(_animationRoot, _spriteRenderer));
        _context = new LifecycleAnimationContext(transform, _targets);
    }

    /// <summary>
    /// Aggiunge un satellite alle animazioni del personaggio. La posa a riposo è catturata ORA: registrare
    /// un oggetto già spostato da qualcun altro ne falserebbe il riposo.
    ///
    /// Registrazione tardiva: l'ordine fra l'<c>Awake</c> di chi registra e l'<c>OnEnable</c> di chi avvia
    /// lo spawn non è garantito, quindi se una fase è già stata preparata il satellite la recupera subito —
    /// altrimenti resterebbe visibile in superficie mentre il corpo è sott'acqua.
    /// </summary>
    public void RegisterSatellite(Transform pivot, SpriteRenderer renderer = null)
    {
        if (pivot == null) return;
        EnsureRestPoseCaptured();

        for (int i = 0; i < _targets.Count; i++)
            if (_targets[i].Pivot == pivot) return;

        if (renderer == null) renderer = pivot.GetComponentInChildren<SpriteRenderer>();

        var target = LifecycleAnimationTarget.Capture(pivot, renderer);
        _targets.Add(target);

        if (_activeAnimation == null) return;

        _activeAnimation.PrepareTarget(target);
        if (IsPlaying) _ = _activeAnimation.PlayTarget(target);
    }

    public void UnregisterSatellite(Transform pivot)
    {
        if (pivot == null) return;

        // Si parte da 1: l'indice 0 è il corpo e non è rimovibile.
        for (int i = 1; i < _targets.Count; i++)
        {
            if (_targets[i].Pivot != pivot) continue;
            _targets.RemoveAt(i);
            return;
        }
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

        if (!TryResolveAnimation(phase, out LifecycleAnimationSO animation)) return;

        BeginPhase(phase, animation);
    }

    public async Awaitable PlayAsync(LifecyclePhase phase, CancellationToken token)
    {
        EnsureRestPoseCaptured();

        if (!TryResolveAnimation(phase, out LifecycleAnimationSO animation)) return;

        BeginPhase(phase, animation);

        IsPlaying = true;
        try
        {
            await animation.PlayAsync(_context, token);
        }
        finally
        {
            IsPlaying = false;
        }

        // Fuori dal finally: una fase cancellata non è una fase completata.
        _activeAnimation = null;
        OnPhaseCompleted?.Invoke(phase);
    }

    /// <summary>
    /// Prende possesso dei bersagli e ne applica lo stato iniziale. I tween altrui vengono fermati prima:
    /// uno shake di telegraph o un salto interrotto a metà scrivono sugli stessi <c>localPosition</c> che
    /// l'animazione di lifecycle sta per animare, e se ne contenderebbero il controllo.
    /// </summary>
    private void BeginPhase(LifecyclePhase phase, LifecycleAnimationSO animation)
    {
        for (int i = 0; i < _targets.Count; i++)
            _targets[i].StopActiveTweens();

        _activeAnimation = animation;
        _activePhase = phase;

        animation.Prepare(in _context);
        OnPhaseStarted?.Invoke(phase);
    }

    private bool TryResolveAnimation(LifecyclePhase phase, out LifecycleAnimationSO animation)
    {
        animation = null;

        // Dizionario vuoto: non è mai una configurazione voluta — un prefab senza animazioni di
        // lifecycle non ha nemmeno questo componente. In build indica che i dati serializzati sono
        // andati persi sul clone (vedi SerializedDictionary.OnAfterDeserialize, che in player
        // svuota la lista di backing dopo la prima deserializzazione).
        if (_animations == null || _animations.Count == 0)
        {
            Debug.LogWarning(
                $"[{nameof(CharacterLifecycleAnimator)}] '{name}': dizionario animazioni vuoto, " +
                $"fase {phase} ignorata.", this);
            return false;
        }

        // Fase non configurata: silenzioso, è la configurazione voluta (non tutti i personaggi
        // hanno un'animazione per ogni fase).
        if (!_animations.TryGetValue(phase, out animation))
            return false;

        // Entry presente ma senza SO: quasi certamente un wiring dimenticato, non un'assenza voluta.
        if (animation == null)
        {
            Debug.LogWarning(
                $"[{nameof(CharacterLifecycleAnimator)}] '{name}': la fase {phase} è nel dizionario " +
                "ma non ha una LifecycleAnimationSO assegnata.", this);
            return false;
        }

        return true;
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

        for (int i = 0; i < _targets.Count; i++)
        {
            _targets[i].StopActiveTweens();
            _targets[i].ResetToRest();
        }

        _activeAnimation = null;
    }
}
