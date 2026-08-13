using UnityEngine;

/// <summary>
/// Un effetto particellare prestabile dal pool. Non decide da solo quando finire: e' il
/// <see cref="VfxDirector"/> a rimetterlo nel pool dopo <see cref="MaxLifetime"/>.
///
/// L'emissione non parte su OnAcquired ma su <see cref="Play"/>: la pool attiva l'oggetto
/// prima che il consumatore lo abbia posizionato, e un ParticleSystem in playOnAwake
/// emetterebbe un frame nel punto sbagliato.
/// </summary>
public class VFXController : PooledBehaviour
{
    [Tooltip("Solo per le istanze NON poolate (piazzate a mano in scena): si autodistruggono a fine effetto")]
    [SerializeField] private bool _autoDestroy = true;

    private ParticleSystem[] _particles;
    // Il loop autorato sul prefab: StopEmitting lo azzera, e su un oggetto riciclato non tornerebbe mai.
    private bool[] _authoredLoop;
    // Scala autorata di ogni sistema e se quel sistema legge la scala del root o la propria (vedi ApplyScale).
    private Vector3[] _particleRestScale;
    private bool[] _scalesFromHierarchy;
    private Vector3 _restScale;
    private bool _initialized;

    /// <summary>Durata oltre la quale l'effetto e' certamente finito. Calcolata una volta sola.</summary>
    public float MaxLifetime { get; private set; }

    /// <summary>Incrementato a ogni Play: permette alle continuazioni asincrone di accorgersi
    /// che l'istanza e' stata nel frattempo rilasciata e riusata per un altro effetto.</summary>
    public uint PlayId { get; private set; }

    /// <summary>Se valorizzato, il VfxDirector copia la posizione del target ogni LateUpdate.</summary>
    public Transform FollowTarget { get; set; }

    /// <summary>Handle dell'effetto persistente in corso, o None. Serve a ripulire la mappa dei
    /// persistenti anche quando l'istanza viene rilasciata da un percorso diverso dallo stop esplicito.</summary>
    public VfxHandle Handle { get; set; }

    private void Awake() => Initialize();

    private void Start()
    {
        // Istanza non poolata: vale il vecchio contratto di autodistruzione.
        if (Releaser == null && _autoDestroy)
            Destroy(gameObject, MaxLifetime);
    }

    public override void OnAcquired()
    {
        Initialize();
        base.OnAcquired();
        StopAndClear();
    }

    public override void OnReleased()
    {
        Initialize();
        StopAndClear();

        FollowTarget = null;
        Handle = VfxHandle.None;
        ApplyScale(1f);   // anche i figli: un'istanza riciclata erediterebbe la scala del cue precedente

        base.OnReleased();
    }

    /// <summary>
    /// Applica la scala richiesta dal cue. Non basta scalare il root: un ParticleSystem in
    /// <see cref="ParticleSystemScalingMode.Local"/> (il default di questo progetto) usa SOLO la scala
    /// del proprio Transform e ignora quella dei parent, quindi su un prefab con i sistemi sui figli
    /// — la forma di gran lunga più comune — la scala scritta sul root verrebbe scartata in silenzio.
    ///
    /// I sistemi in <see cref="ParticleSystemScalingMode.Hierarchy"/> vanno invece lasciati stare:
    /// ereditano già il root, e riapplicare la scala anche a loro darebbe scale².
    /// </summary>
    public void ApplyScale(float scale)
    {
        Initialize();

        transform.localScale = _restScale * scale;   // copre anche i figli non particellari (luci, trail)

        for (int i = 0; i < _particles.Length; i++)
        {
            if (_scalesFromHierarchy[i]) continue;

            Transform particleTransform = _particles[i].transform;
            if (particleTransform == transform) continue;   // già scalato sopra

            particleTransform.localScale = _particleRestScale[i] * scale;
        }
    }

    /// <summary>Avvia l'effetto. Da chiamare dopo aver posizionato e scalato l'istanza.</summary>
    public void Play()
    {
        Initialize();
        PlayId++;

        for (int i = 0; i < _particles.Length; i++)
        {
            ParticleSystem.MainModule main = _particles[i].main;
            main.loop = _authoredLoop[i];
        }

        // Play(false): i sistemi figli sono gia' tutti nell'array, avviarli due volte li resetterebbe.
        for (int i = 0; i < _particles.Length; i++)
            _particles[i].Play(false);
    }

    /// <summary>
    /// Chiude l'effetto lasciando esaurire le particelle gia' vive. Non rimette nel pool:
    /// il rilascio arriva dal director dopo MaxLifetime, altrimenti l'effetto sparirebbe di colpo.
    /// </summary>
    public void StopEmitting()
    {
        Initialize();

        for (int i = 0; i < _particles.Length; i++)
        {
            ParticleSystem.MainModule main = _particles[i].main;
            main.loop = false;
        }

        for (int i = 0; i < _particles.Length; i++)
            _particles[i].Stop(false, ParticleSystemStopBehavior.StopEmitting);
    }

    private void StopAndClear()
    {
        for (int i = 0; i < _particles.Length; i++)
            _particles[i].Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        _particles = GetComponentsInChildren<ParticleSystem>(true);
        _authoredLoop = new bool[_particles.Length];
        _particleRestScale = new Vector3[_particles.Length];
        _scalesFromHierarchy = new bool[_particles.Length];
        _restScale = transform.localScale;

        float max = 0f;

        for (int i = 0; i < _particles.Length; i++)
        {
            ParticleSystem.MainModule main = _particles[i].main;
            _authoredLoop[i] = main.loop;
            _particleRestScale[i] = _particles[i].transform.localScale;
            _scalesFromHierarchy[i] = main.scalingMode == ParticleSystemScalingMode.Hierarchy;

            float lifetime = main.startLifetime.constantMax + main.duration;
            if (lifetime > max) max = lifetime;
        }

        MaxLifetime = Mathf.Max(max, 0.1f);
    }
}
