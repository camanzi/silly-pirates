using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The scene's VFX director: it listens to the cue channels and plays effects using one pool per prefab.
///
/// The twin of <see cref="AudioDirector"/>: a scene MonoBehaviour that owns its objects directly (never
/// inside a ScriptableObject) and subscribes to the channels in OnEnable/OnDisable.
/// Like audio it never blocks the turn loop, but unlike audio it never steals an active effect when the
/// pool is exhausted: a VFX cut off halfway is noticeable, a missing one is not.
/// </summary>
public class VfxDirector : MonoBehaviour
{
    private const float ReleaseGraceSeconds = 0.05f;

    [Header("Pool")]
    [Tooltip("Instances created at start-up for each prefab. 0 by default: the VFX prefabs are many and varied, and prewarming them all would be a scene-load hitch")]
    [Min(0)] [SerializeField] private int _prewarmPerPrefab = 0;
    [Tooltip("Hard cap on simultaneous instances per prefab. Beyond it, cues are dropped")]
    [Min(1)] [SerializeField] private int _maxPerPrefab = 8;

    private PrefabPoolRegistry<VFXController> _registry;
    private Transform _poolRoot;

    // Only the instances currently following a Transform: the others cost nothing per frame.
    private readonly List<VFXController> _followers = new();

    private readonly Dictionary<Guid, VFXController> _persistent = new();

    private void Awake() => EnsureRegistry();

    /// <summary>
    /// Lazy and idempotent: a cue can arrive from another object's OnEnable, which Unity may run before
    /// this director's Awake — that is the case for a character spawning, which raises its lifecycle VFX
    /// the moment it is activated. The same reason as
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
        // LateUpdate and not Update: PrimeTween moves the visuals in Update, so the position has to be
        // copied afterwards.
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
                // The target was destroyed mid-effect: the instance goes back into the pool.
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

        // An already active handle means the same persistent effect applied twice: a no-op.
        if (cue.Handle.IsValid && _persistent.ContainsKey(cue.Handle.Id)) return;

        VFXController vfx = _registry.Acquire(cue.Prefab);

        if (vfx == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[VfxDirector] Pool for '{cue.Prefab.name}' exhausted ({_maxPerPrefab} instances): cue dropped.", this);
#endif
            return;
        }

        // A scale of 0 comes from payloads built by hand without the factory methods: it reads as "default".
        float scale = cue.Scale > 0f ? cue.Scale : 1f;
        vfx.ApplyScale(scale);   // and not transform.localScale: child systems do not inherit it (VFXController.ApplyScale)
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
            // Persistent: no automatic release, it lives until the stop arrives.
            vfx.Handle = cue.Handle;
            _persistent[cue.Handle.Id] = vfx;
            return;
        }

        ScheduleRelease(vfx);
    }

    // Called by the VfxCueChannelListener when a stop event is raised. 
    public void HandleStop(VfxHandle handle)
    {
        // A dictionary miss is a silent no-op: a double stop is a normal case.
        if (!handle.IsValid) return;
        if (!_persistent.TryGetValue(handle.Id, out VFXController vfx)) return;

        _persistent.Remove(handle.Id);

        if (vfx == null || !vfx.IsInUse) return;

        vfx.Handle = VfxHandle.None;
        vfx.StopEmitting();
        ScheduleRelease(vfx);
    }

    /// <summary>
    /// Returns an effect to the pool once it is certainly over.
    /// No per-frame cost: a single async continuation per instance.
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

    /// <summary>Centralized release: it removes the instance from every register before handing it back.</summary>
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
