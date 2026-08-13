using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Regista VFX di scena: ascolta i canali di cue e riproduce gli effetti usando un pool per prefab.
///
/// Gemello di <see cref="AudioDirector"/>: MonoBehaviour di scena che possiede direttamente i propri
/// oggetti (mai dentro uno ScriptableObject) e si iscrive ai canali in OnEnable/OnDisable.
/// Come l'audio non blocca mai il turn loop, ma a differenza dell'audio non ruba mai un effetto
/// attivo quando la pool e' esaurita: un VFX troncato a meta' si vede, uno mancante no.
/// </summary>
public class VfxDirector : MonoBehaviour
{
    private const float ReleaseGraceSeconds = 0.05f;

    [Header("Pool")]
    [Tooltip("Istanze create all'avvio per ogni prefab. 0 di default: i prefab VFX sono tanti e vari, prewarmarli tutti sarebbe un hitch di caricamento scena")]
    [Min(0)] [SerializeField] private int _prewarmPerPrefab = 0;
    [Tooltip("Tetto massimo di istanze simultanee per ogni prefab. Oltre questo i cue vengono scartati")]
    [Min(1)] [SerializeField] private int _maxPerPrefab = 8;

    private PrefabPoolRegistry<VFXController> _registry;
    private Transform _poolRoot;

    // Solo le istanze che stanno inseguendo un Transform: le altre non costano nulla per frame.
    private readonly List<VFXController> _followers = new();

    private readonly Dictionary<Guid, VFXController> _persistent = new();

    private void Awake() => EnsureRegistry();

    /// <summary>
    /// Lazy e idempotente: un cue puo' arrivare dall'OnEnable di un altro oggetto, che Unity puo'
    /// eseguire prima dell'Awake di questo director — e' il caso dello spawn di un personaggio, che
    /// alza i VFX di lifecycle appena viene attivato. Stesso motivo di
    /// <c>CharacterLifecycleAnimator.EnsureRestPoseCaptured</c>.
    /// </summary>
    private void EnsureRegistry()
    {
        if (_registry != null) return;

        _poolRoot = new GameObject("VfxPool").transform;
        _poolRoot.SetParent(transform, false);

        _registry = new PrefabPoolRegistry<VFXController>(_poolRoot, _prewarmPerPrefab, _maxPerPrefab);
    }

    private void OnDisable()
    {
        StopEverything();
    }

    private void OnDestroy() => _registry?.Clear();

    private void LateUpdate()
    {
        // LateUpdate e non Update: PrimeTween muove i visual in Update, la posizione va copiata dopo.
        if (_followers.Count == 0) return;

        for (int i = _followers.Count - 1; i >= 0; i--)
        {
            VFXController vfx = _followers[i];

            if (vfx == null || !vfx.IsInUse)
            {
                _followers.RemoveAt(i);
                continue;
            }

            if (vfx.FollowTarget == null)
            {
                // Il bersaglio e' stato distrutto a meta' effetto: l'istanza torna nel pool.
                _followers.RemoveAt(i);
                ReleaseVfx(vfx);
                continue;
            }

            vfx.transform.position = vfx.FollowTarget.position;
        }
    }

    // Called by the VfxCueChannelListener when a cue event is raised.
    public void HandleCue(VfxCue cue)
    {
        if (cue.Prefab == null) return;

        EnsureRegistry();

        // Handle gia' attivo = doppia applicazione dello stesso effetto persistente: no-op.
        if (cue.Handle.IsValid && _persistent.ContainsKey(cue.Handle.Id)) return;

        VFXController vfx = _registry.Acquire(cue.Prefab);

        if (vfx == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[VfxDirector] Pool di '{cue.Prefab.name}' esaurita ({_maxPerPrefab} istanze): cue scartato.", this);
#endif
            return;
        }

        // Uno scale a 0 viene dai payload costruiti a mano senza i factory method: si legge come "default".
        float scale = cue.Scale > 0f ? cue.Scale : 1f;
        vfx.ApplyScale(scale);   // e non transform.localScale: i sistemi figli non ereditano (VFXController.ApplyScale)
        vfx.transform.rotation = cue.Rotation;

        if (cue.FollowTarget != null)
        {
            vfx.FollowTarget = cue.FollowTarget;
            vfx.transform.position = cue.FollowTarget.position;
            _followers.Add(vfx);
        }
        else
        {
            vfx.transform.position = cue.Position;
        }

        vfx.Play();

        if (cue.Handle.IsValid)
        {
            // Persistente: nessun rilascio automatico, vive finche' non arriva lo stop.
            vfx.Handle = cue.Handle;
            _persistent[cue.Handle.Id] = vfx;
            return;
        }

        ScheduleRelease(vfx);
    }

    // Called by the VfxCueChannelListener when a stop event is raised. 
    public void HandleStop(VfxHandle handle)
    {
        // Miss del dizionario = no-op silenzioso: il doppio stop e' un caso normale.
        if (!handle.IsValid) return;
        if (!_persistent.TryGetValue(handle.Id, out VFXController vfx)) return;

        _persistent.Remove(handle.Id);

        if (vfx == null || !vfx.IsInUse) return;

        vfx.Handle = VfxHandle.None;
        vfx.StopEmitting();
        ScheduleRelease(vfx);
    }

    /// <summary>
    /// Riporta nel pool un effetto quando e' certamente finito.
    /// Nessun costo per frame: una sola continuazione asincrona per istanza.
    /// </summary>
    private async void ScheduleRelease(VFXController vfx)
    {
        uint playId = vfx.PlayId;
        float delay = vfx.MaxLifetime + ReleaseGraceSeconds;

        try
        {
            await Awaitable.WaitForSecondsAsync(delay, destroyCancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (vfx == null || !vfx.IsInUse || vfx.PlayId != playId) return;
        ReleaseVfx(vfx);
    }

    /// <summary>Rilascio centralizzato: sfila l'istanza da ogni registro prima di restituirla.</summary>
    private void ReleaseVfx(VFXController vfx)
    {
        if (vfx == null || !vfx.IsInUse) return;

        if (vfx.Handle.IsValid) _persistent.Remove(vfx.Handle.Id);

        int index = _followers.IndexOf(vfx);
        if (index >= 0) _followers.RemoveAt(index);

        _registry.Release(vfx);
    }

    private void StopEverything()
    {
        _followers.Clear();
        _persistent.Clear();
        _registry?.ReleaseAll();
    }
}
