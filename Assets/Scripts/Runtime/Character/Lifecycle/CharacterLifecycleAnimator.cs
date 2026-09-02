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
    /// Binario indipendente da <see cref="ActivePhase"/>/<see cref="IsPlaying"/>: una transizione (Spawn,
    /// Leave) e un loop (Idle, domani PostDeath) non sono mai attivi insieme, ma nessun consumatore
    /// esistente deve accorgersi del loop, quindi i due stati restano separati invece di essere unificati.
    /// </summary>
    public bool IsLoopingAnimationPlaying => _loopingAnimation != null;

    /// <summary>Fase in loop attiva; <c>null</c> quando il personaggio è a riposo rispetto a questo binario.</summary>
    public LifecyclePhase? LoopingPhase => _loopingAnimation != null ? _loopingPhase : null;

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

    // Per-istanza e non nella SO (condivisa fra prefab): tiene gli handle dei VFX persistenti della fase
    // in corso. Riusata fase dopo fase, così spawn ripetuti non allocano.
    private readonly LifecycleVfxSession _vfxSession = new();

    // Binario loop, separato dai campi sopra e non riusato: _vfxSession.StopAll() gira a ogni BeginPhase
    // (con la guardia "resuming"), condividerla spegnerebbe i VFX del loop a ogni Spawn e intreccerebbe
    // i due binari che il piano vuole indipendenti.
    private readonly LifecycleTweenSession _loopTweenSession = new();
    private readonly LifecycleVfxSession _loopVfxSession = new();

    private LifecycleAnimationContext _context;
    private bool _restPoseCaptured;

    // true da Start in poi. Prima di quel momento la scena si sta ancora inizializzando: alzare un cue
    // significa parlare a un director che può non aver ancora fatto Awake, o a un listener non ancora
    // iscritto — e in quel secondo caso il cue si perde in silenzio, senza nessun errore. Lo stato
    // visivo invece si applica subito: Prepare non comunica con nessuno.
    private bool _initialized;
    private bool _pendingVfxBegin;

    // Non-null solo fra l'inizio di una fase e il suo completamento: serve ad agganciare alla fase in
    // corso un satellite che si registra in ritardo.
    private LifecycleAnimationSO _activeAnimation;
    private LifecyclePhase _activePhase;

    // Specchio dei tre campi sopra ma per il binario loop. _pendingLoopStart replica la stessa finestra di
    // inizializzazione di _pendingVfxBegin: StartLoop può essere chiamato da OnEnable, prima di Start.
    private LoopingLifecycleAnimationSO _loopingAnimation;
    private LifecyclePhase _loopingPhase;
    private bool _pendingLoopStart;

    // Bool e non contatore: i tre punti di sospensione del progetto (salto, shake di telegraph, orbita
    // dello scettro) non sono mai concorrenti sullo stesso personaggio, e StopLoop() lo azzera comunque a
    // ogni transizione — non serve un refcount per uno stato che è già garantito non annidarsi.
    private bool _loopSuspended;

    private void Awake() => EnsureRestPoseCaptured();

    /// <summary>
    /// Fine della finestra di inizializzazione: è l'unico momento garantito dopo TUTTI gli Awake e gli
    /// OnEnable della scena, ed è comunque nello stesso frame in cui il personaggio è stato attivato,
    /// prima che venga renderizzato — quindi il VFX rimandato qui non si vede partire in ritardo.
    /// </summary>
    private void Start()
    {
        _initialized = true;
        FlushPendingVfxBegin();
        FlushPendingLoopStart();

        // Copre il personaggio senza uno slot Spawn configurato: Play(Spawn) gira in OnEnable e imposta
        // _activeAnimation in modo sincrono (vedi commento su OnPhaseCompleted), quindi se a questo punto
        // non c'è né una transizione né un loop già in corso, nessuno lo avvierà mai da solo.
        if (_activeAnimation == null && _loopingAnimation == null)
            StartLoop(LifecyclePhase.Idle);
    }

    /// <summary>Alza i VFX di <see cref="LifecycleVfxStage.Prepare"/> rimandati da <see cref="BeginPhase"/>.</summary>
    private void FlushPendingVfxBegin()
    {
        if (!_pendingVfxBegin) return;

        _pendingVfxBegin = false;
        _activeAnimation?.BeginVfx(_vfxSession, in _context);
    }

    /// <summary>Avvia il loop rimandato da <see cref="StartLoop"/> quando chiamato prima di <see cref="Start"/>.</summary>
    private void FlushPendingLoopStart()
    {
        if (!_pendingLoopStart) return;

        _pendingLoopStart = false;
        if (_loopingAnimation != null) BeginLoopPlayback(_loopingAnimation);
    }

    /// <summary>
    /// Rete di sicurezza per la fase preparata e mai giocata: <see cref="PrepareHidden"/> apre la sessione
    /// VFX, e se il personaggio viene distrutto o disattivato durante l'intro nessun <c>finally</c> di
    /// <see cref="PlayAsync"/> la chiuderebbe. Il fallback di ultima istanza resta
    /// <c>VfxDirector.OnDisable</c> → <c>StopEverything()</c>, ma non deve essere l'unico.
    /// </summary>
    private void OnDisable()
    {
        _vfxSession.StopAll();
        _pendingVfxBegin = false;   // disattivato prima di Start: al risveglio non deve accendere nulla
        StopLoop();
    }

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

        // I due binari sono mutuamente esclusivi (BeginPhase ferma sempre il loop prima di una
        // transizione), ma un satellite può registrarsi mentre gira l'uno o l'altro: stesso recupero
        // per entrambi, altrimenti resterebbe indietro finché la fase/il loop in corso non ricomincia.
        if (_activeAnimation != null)
        {
            _activeAnimation.PrepareTarget(target);
            if (IsPlaying) _ = _activeAnimation.PlayTarget(target);
        }

        if (_loopingAnimation != null)
        {
            _loopingAnimation.PrepareTarget(target);
            if (!_loopSuspended) _loopTweenSession.Track(_loopingAnimation.StartLoopOn(target));
        }
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

        // Un Play() scritto per errore su una fase in loop (es. Idle) non deve MAI finire su
        // `await bodyTween`: quel tween non torna mai, e l'await bloccherebbe per sempre sia questo
        // metodo sia IsPlaying. Si ridirige sul binario giusto invece di crashare o impiccarsi.
        if (animation is LoopingLifecycleAnimationSO)
        {
            StartLoop(phase);
            return;
        }

        BeginPhase(phase, animation);

        IsPlaying = true;
        try
        {
            // IsPlaying è già vero qui sopra, sincrono: WaitUntilIdleAsync (SpawnAlliesCommand) non deve
            // mai vedere l'animator a riposo fra il Play e la prima attesa.
            //
            // Play() è async void e gira sincrono dentro OnEnable: senza questa attesa anche lo stage
            // Movement partirebbe verso sistemi non ancora inizializzati. Costo zero a scena avviata.
            while (!_initialized)
                await Awaitable.NextFrameAsync(token);

            FlushPendingVfxBegin();

            await animation.PlayAsync(_context, _vfxSession, token);
        }
        finally
        {
            IsPlaying = false;

            // Qui e non dentro la SO: copre la cancellazione del token (personaggio distrutto a metà
            // emersione), che altrimenti lascerebbe i persistenti accesi per sempre.
            _vfxSession.StopAll();
        }

        // Fuori dal finally: una fase cancellata non è una fase completata.
        _activeAnimation = null;
        OnPhaseCompleted?.Invoke(phase);

        // Un solo punto che copre sia lo spawn normale sia quello dell'intro al combattimento (che passa
        // comunque da qui), invece di ramificare "se è uno spawn, avvia l'idle" in più posti.
        if (animation.StartsLoopOnComplete) StartLoop(animation.LoopPhaseOnComplete);
    }

    /// <summary>
    /// Prende possesso dei bersagli e ne applica lo stato iniziale. I tween altrui vengono fermati prima:
    /// uno shake di telegraph o un salto interrotto a metà scrivono sugli stessi <c>localPosition</c> che
    /// l'animazione di lifecycle sta per animare, e se ne contenderebbero il controllo.
    ///
    /// I VFX di <see cref="LifecycleVfxStage.Prepare"/> partono qui, subito dopo lo stato iniziale — ma
    /// SOLO alla prima preparazione della fase. <see cref="PrepareHidden"/> e <see cref="PlayAsync"/>
    /// passano entrambi di qui per la stessa fase durante l'intro al combattimento: senza la guardia le
    /// bolle di un'emersione ripartirebbero da capo nel momento in cui il mostro comincia a salire.
    ///
    /// Se la scena si sta ancora inizializzando (questo metodo è raggiunto dall'OnEnable del personaggio)
    /// i VFX vengono messi in coda e alzati da <see cref="Start"/>: lo stato visivo non può aspettare,
    /// i cue verso gli altri sistemi sì.
    /// </summary>
    private void BeginPhase(LifecyclePhase phase, LifecycleAnimationSO animation)
    {
        // In testa e non delegato a StopActiveTweens sotto: quello uccide comunque i tween del loop
        // (sono sugli stessi Transform), ma non gli handle registrati in _loopTweenSession, non i VFX
        // del loop e non il flag di sospensione — StopLoop() è l'unico che chiude tutti e tre insieme.
        StopLoop();

        // Anche il begin rimandato conta come sessione già aperta: senza, PrepareHidden e PlayAsync
        // sulla stessa fase lo metterebbero in coda due volte.
        bool resuming = (_vfxSession.IsOpen || _pendingVfxBegin)
                        && _activeAnimation == animation && _activePhase == phase;

        // Fase diversa da quella preparata: i persistenti rimasti aperti non le appartengono.
        if (!resuming)
        {
            _vfxSession.StopAll();
            _pendingVfxBegin = false;
        }

        for (int i = 0; i < _targets.Count; i++)
            _targets[i].StopActiveTweens();

        _activeAnimation = animation;
        _activePhase = phase;

        animation.Prepare(in _context);

        if (!resuming)
        {
            if (_initialized) animation.BeginVfx(_vfxSession, in _context);
            else _pendingVfxBegin = true;
        }

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

        _vfxSession.StopAll();
        _pendingVfxBegin = false;
        _activeAnimation = null;

        StopLoop();
    }

    /// <summary>
    /// Avvia il binario loop sulla fase indicata — oggi solo <see cref="LifecyclePhase.Idle"/>, domani
    /// anche un <c>PostDeath</c>: il meccanismo non sa e non deve sapere quale fase gira sopra di lui.
    /// Stessa risoluzione di <see cref="PlayAsync"/> (<see cref="TryResolveAnimation"/>, stessi log), ma
    /// NON passa mai da <c>LifecycleAnimationSO.PlayAsync</c>: quel binario presuppone un tween che
    /// finisce, un loop no.
    /// </summary>
    public void StartLoop(LifecyclePhase phase)
    {
        EnsureRestPoseCaptured();

        if (!TryResolveAnimation(phase, out LifecycleAnimationSO animation)) return;

        if (animation is not LoopingLifecycleAnimationSO loopingAnimation)
        {
            Debug.LogWarning(
                $"[{nameof(CharacterLifecycleAnimator)}] '{name}': la fase {phase} non è una " +
                $"{nameof(LoopingLifecycleAnimationSO)}, StartLoop ignorato.", this);
            return;
        }

        // Chiude un loop precedente (VFX, tween, sospensione) prima di prenderne possesso: stesso motivo
        // per cui BeginPhase ferma i tween altrui prima di animare gli stessi pivot.
        StopLoop();

        _loopingAnimation = loopingAnimation;
        _loopingPhase = phase;

        loopingAnimation.Prepare(in _context);

        // Stessa finestra di inizializzazione di BeginPhase/_pendingVfxBegin: un cue alzato prima di Start
        // può parlare a un director non ancora sveglio o perdersi su un listener non ancora iscritto.
        if (!_initialized)
        {
            _pendingLoopStart = true;
            return;
        }

        BeginLoopPlayback(loopingAnimation);
    }

    /// <summary>Alza Prepare+Movement e avvia i tween su tutti i bersagli. Condiviso fra <see cref="StartLoop"/> (percorso sincrono) e <see cref="FlushPendingLoopStart"/> (percorso rimandato).</summary>
    private void BeginLoopPlayback(LoopingLifecycleAnimationSO animation)
    {
        animation.BeginVfx(_loopVfxSession, in _context);
        animation.RaiseMovementVfx(in _context, _loopVfxSession);

        _loopTweenSession.Begin();
        animation.StartLoops(in _context, _loopTweenSession);
    }

    /// <summary>
    /// Ferma il binario loop: VFX (stage End), tween e registrazione dell'animazione, con ripristino della
    /// sola posizione dei bersagli. Chiamata da <see cref="BeginPhase"/>, <see cref="ResetToRest"/> e
    /// <c>OnDisable</c> per rimettere il personaggio "a riposo" rispetto a questo binario.
    ///
    /// Azzera anche <see cref="_loopSuspended"/>, ma quello è un ripiego, non una rete su cui contare: si
    /// attiva solo quando il personaggio cambia fase, e chi sospende resta tipicamente vivo e a riposo per
    /// tutto il prestito. Ogni <see cref="SuspendLoop"/> deve avere il suo <see cref="ResumeLoop"/> su un
    /// percorso che gira davvero (cfr. <see cref="MultiStepAbilityStepSO.EndPartShake"/>).
    /// </summary>
    public void StopLoop()
    {
        EnsureRestPoseCaptured();

        if (_loopingAnimation == null)
        {
            _loopSuspended = false;
            return;
        }

        _loopingAnimation.RaiseEndVfx(in _context, _loopVfxSession);

        _loopTweenSession.StopAll();
        _loopVfxSession.StopAll();
        RestoreLoopPose();

        _loopingAnimation = null;
        _pendingLoopStart = false;
        _loopSuspended = false;
    }

    /// <summary>
    /// Sospende il SOLO movimento del loop: usata dai comandi che tweenano lo stesso <c>localPosition</c>
    /// di un bersaglio registrato (salto, shake di telegraph, orbita dello scettro) e che altrimenti se ne
    /// contenderebbero il controllo. Animazione e VFX restano registrati — non è un conflitto di effetti,
    /// solo di transform — così <see cref="ResumeLoop"/> riparte senza rigiocare Prepare/Movement.
    /// </summary>
    public void SuspendLoop()
    {
        EnsureRestPoseCaptured();

        if (_loopingAnimation == null || _loopSuspended) return;

        _loopSuspended = true;
        _loopTweenSession.StopAll();
        RestoreLoopPose();
    }

    /// <summary>
    /// Controparte di <see cref="SuspendLoop"/>: riavvia i tween su tutti i bersagli correnti. Non serve
    /// distinguere quelli registrati durante la sospensione: <see cref="RegisterSatellite"/> li aggiunge
    /// già a <c>_targets</c>, e <see cref="LoopingLifecycleAnimationSO.StartLoops"/> li itera tutti.
    ///
    /// Ripristina la posa PRIMA di ripartire, gemello di quello che <see cref="SuspendLoop"/> fa in uscita:
    /// la sospensione è un prestito del transform, e chi lo restituisce non deve sapere dov'era il riposo.
    /// Serve davvero — <c>Tween.Stop()</c> di PrimeTween è un kill, non un rewind: uno shake infinito
    /// fermato a metà oscillazione lascia il pivot sfasato, e i tween del loop ripartono dal valore
    /// corrente, fissando quello sfasamento per sempre.
    /// </summary>
    public void ResumeLoop()
    {
        if (!_loopSuspended || _loopingAnimation == null) return;

        _loopSuspended = false;
        RestoreLoopPose();
        _loopTweenSession.Begin();
        _loopingAnimation.StartLoops(in _context, _loopTweenSession);
    }

    /// <summary>
    /// Ripristina la sola posizione dei bersagli — MAI la scala. A differenza di
    /// <see cref="LifecycleAnimationTarget.ResetPoseToRest"/>, che tocca anche <c>localScale</c>, il loop
    /// non anima e non deve appropriarsi della scala: la stanno già usando
    /// <see cref="JumpSquashStretchHelper"/> e i comandi legacy di squash-stretch, e un ripristino qui la
    /// riporterebbe a riposo a metà del loro tween.
    /// </summary>
    private void RestoreLoopPose()
    {
        for (int i = 0; i < _targets.Count; i++)
        {
            Transform pivot = _targets[i].Pivot;
            if (pivot != null) pivot.localPosition = _targets[i].RestLocalPosition;
        }
    }
}
