using UnityEngine;

/// <summary>
/// A particle effect that can be borrowed from the pool. It does not decide on its own when to end: it is
/// the <see cref="VfxDirector"/> that returns it to the pool after <see cref="MaxLifetime"/>.
///
/// Emission does not start on OnAcquired but on <see cref="Play"/>: the pool activates the object before
/// the consumer has positioned it, and a ParticleSystem with playOnAwake would emit one frame in the
/// wrong place.
/// </summary>
public class VFXController : PooledBehaviour
{
    [Tooltip("Only for NON-pooled instances (placed by hand in the scene): they self-destruct when the effect ends")]
    [SerializeField] private bool _autoDestroy = true;

    private ParticleSystem[] _particles;
    // The loop as authored on the prefab: StopEmitting clears it, and on a recycled object it would never come back.
    private bool[] _authoredLoop;
    // Each system's authored scale, and whether that system reads the root's scale or its own (see ApplyScale).
    private Vector3[] _particleRestScale;
    private bool[] _scalesFromHierarchy;
    private Vector3 _restScale;
    private bool _initialized;

    /// <summary>The duration past which the effect is certainly over. Computed once and only once.</summary>
    public float MaxLifetime { get; private set; }

    /// <summary>Incremented on every Play: it lets async continuations notice that the instance has
    /// meanwhile been released and reused for another effect.</summary>
    public uint PlayId { get; private set; }

    /// <summary>When set, the VfxDirector copies the target's position every LateUpdate.</summary>
    public Transform FollowTarget { get; set; }

    /// <summary>Handle of the persistent effect in flight, or None. It is what lets the map of persistent
    /// effects be cleaned up even when the instance is released through a path other than an explicit stop.</summary>
    public VfxHandle Handle { get; set; }

    private void Awake() => Initialize();

    private void Start()
    {
        // Non-pooled instance: the old self-destruction contract applies.
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
        ApplyScale(1f);   // children too: a recycled instance would inherit the previous cue's scale

        base.OnReleased();
    }

    /// <summary>
    /// Applies the scale requested by the cue. Scaling the root is not enough: a ParticleSystem in
    /// <see cref="ParticleSystemScalingMode.Local"/> (this project's default) uses ONLY its own Transform's
    /// scale and ignores the parents', so on a prefab with the systems on the children — by far the most
    /// common shape — the scale written on the root would be silently discarded.
    ///
    /// Systems in <see cref="ParticleSystemScalingMode.Hierarchy"/> must be left alone instead: they
    /// already inherit the root, and reapplying the scale to them as well would give scale².
    /// </summary>
    public void ApplyScale(float scale)
    {
        Initialize();

        transform.localScale = _restScale * scale;   // also covers non-particle children (lights, trails)

        for (int i = 0; i < _particles.Length; i++)
        {
            if (_scalesFromHierarchy[i]) continue;

            Transform particleTransform = _particles[i].transform;
            if (particleTransform == transform) continue;   // already scaled above

            particleTransform.localScale = _particleRestScale[i] * scale;
        }
    }

    /// <summary>Starts the effect. To be called after the instance has been positioned and scaled.</summary>
    public void Play()
    {
        Initialize();
        PlayId++;

        for (int i = 0; i < _particles.Length; i++)
        {
            ParticleSystem.MainModule main = _particles[i].main;
            main.loop = _authoredLoop[i];
        }

        // Play(false): the child systems are all in the array already, starting them twice would reset them.
        for (int i = 0; i < _particles.Length; i++)
            _particles[i].Play(false);
    }

    /// <summary>
    /// Closes the effect, letting the already-alive particles run out. It does not return to the pool:
    /// the release comes from the director after MaxLifetime, otherwise the effect would vanish abruptly.
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
