using System.Collections.Generic;
using PrimeTween;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Detects ship occluder geometry (masts, sails, rigging, crow's nest — tagged via
/// ShipOccluderRegistry) that sits between the action camera and the cue's framed subjects,
/// and dithers it out using Unity's built-in LOD cross-fade dissolve (unity_LODFade), fading it
/// back in once it stops occluding or the action camera releases.
///
/// Fully decoupled from CameraDirector: it only observes _actionCamera.enabled, which
/// CameraDirector already toggles true/false for the whole shot+post-hold window. No event
/// subscriptions to CameraDirector's internals.
/// </summary>
public class ShipOcclusionFader : MonoBehaviour
{
    [Header("Camera Rig (anchors)")]
    [SerializeField] private CinemachineCameraAnchorSO _actionCameraAnchor;
    [SerializeField] private CinemachineTargetGroupAnchorSO _targetGroupAnchor;

    [Header("Dependencies")]
    [SerializeField] private ShipOccluderRegistrySO _occluderRegistry;

    [Header("Fade Settings")]
    [SerializeField] private float _fadeDuration = 0.25f;

    private CinemachineCamera _actionCamera;
    private CinemachineTargetGroup _targetGroup;

    private static readonly int FadeId = Shader.PropertyToID("_Fade");

    private class RendererFadeState
    {
        public Renderer Renderer;
        public MaterialPropertyBlock Mpb;
        public float CurrentFade;
        public float TargetFade;
        public Tween ActiveTween;
    }

    private readonly Dictionary<Renderer, RendererFadeState> _states = new();

    // This catches a ship unregistering without having to modify ShipOccluderRegistrySO (which exposes
    // no OnUnregistered event and is on the do-not-touch list): the list of renderers is kept per known
    // registry and compared every frame against the current one exposed by the registry SO. The fader
    // will become persistent (the Persistent scene), so its own OnDisable will no longer fire between one
    // combat and the next to clean up by itself.
    private readonly Dictionary<ShipOccluderRegistry, List<Renderer>> _rendererOwners = new();
    private readonly List<ShipOccluderRegistry> _staleRegistriesBuffer = new();

    private void OnEnable()
    {
        // Pull-then-subscribe, like CameraDirector: the rig may exist already or arrive later.
        if (_actionCameraAnchor != null)
        {
            _actionCamera = _actionCameraAnchor.Value;
            _actionCameraAnchor.OnValueChanged += HandleActionCameraChanged;
        }
        if (_targetGroupAnchor != null)
        {
            _targetGroup = _targetGroupAnchor.Value;
            _targetGroupAnchor.OnValueChanged += HandleTargetGroupChanged;
        }

        if (_occluderRegistry != null)
        {
            IReadOnlyList<ShipOccluderRegistry> existing = _occluderRegistry.Registries;
            for (int i = 0; i < existing.Count; i++) InitializeRenderers(existing[i]);
            _occluderRegistry.OnRegistered += InitializeRenderers;
        }
    }

    private void OnDisable()
    {
        if (_actionCameraAnchor != null) _actionCameraAnchor.OnValueChanged -= HandleActionCameraChanged;
        if (_targetGroupAnchor != null) _targetGroupAnchor.OnValueChanged -= HandleTargetGroupChanged;
        if (_occluderRegistry != null) _occluderRegistry.OnRegistered -= InitializeRenderers;
    }

    private void HandleActionCameraChanged(CinemachineCamera camera) => _actionCamera = camera;

    private void HandleTargetGroupChanged(CinemachineTargetGroup targetGroup) => _targetGroup = targetGroup;

    /// <summary>
    /// Brings every candidate renderer to "fully visible" instead of leaving it undefined until the first
    /// occlusion evaluation: SetTargetFade would bail out immediately for a freshly created state whose
    /// target already matches the default 0f, without ever writing a MaterialPropertyBlock. Also called
    /// for registries that register later (the ship arriving in the intro, an additively loaded scene).
    /// </summary>
    private void InitializeRenderers(ShipOccluderRegistry registry)
    {
        if (registry == null) return;

        IReadOnlyList<Renderer> renderers = registry.OccluderRenderers;
        if (renderers == null) return;

        var owned = new List<Renderer>(renderers.Count);
        for (int i = 0; i < renderers.Count; i++)
        {
            Renderer candidate = renderers[i];
            if (candidate == null) continue;

            ApplyFade(GetOrCreateState(candidate), 0f);
            owned.Add(candidate);
        }
        _rendererOwners[registry] = owned;
    }

    private void Update()
    {
        PruneUnregisteredShips();

        if (_actionCamera == null || !_actionCamera.enabled)
        {
            FadeAllOut();
            return;
        }

        if (_targetGroup == null || _occluderRegistry == null) return;

        EvaluateOcclusion();
    }

    /// <summary>
    /// Compares the registries being tracked against those currently alive in
    /// ShipOccluderRegistrySO.Registries and frees the renderer state of anything that has unregistered
    /// (a ship destroyed or disabled) — otherwise _states would keep referencing renderers from the
    /// unloaded combat scene for the rest of the session.
    /// </summary>
    private void PruneUnregisteredShips()
    {
        if (_occluderRegistry == null || _rendererOwners.Count == 0) return;

        IReadOnlyList<ShipOccluderRegistry> liveRegistries = _occluderRegistry.Registries;

        _staleRegistriesBuffer.Clear();
        foreach (KeyValuePair<ShipOccluderRegistry, List<Renderer>> kvp in _rendererOwners)
        {
            bool stillLive = false;
            for (int i = 0; i < liveRegistries.Count; i++)
            {
                if (liveRegistries[i] == kvp.Key) { stillLive = true; break; }
            }
            if (!stillLive) _staleRegistriesBuffer.Add(kvp.Key);
        }

        for (int i = 0; i < _staleRegistriesBuffer.Count; i++)
        {
            ShipOccluderRegistry stale = _staleRegistriesBuffer[i];
            List<Renderer> renderers = _rendererOwners[stale];
            for (int r = 0; r < renderers.Count; r++)
            {
                if (_states.TryGetValue(renderers[r], out RendererFadeState state))
                {
                    state.ActiveTween.Stop();
                    _states.Remove(renderers[r]);
                }
            }
            _rendererOwners.Remove(stale);
        }
    }

    private void FadeAllOut()
    {
        if (_states.Count == 0) return;

        foreach (RendererFadeState state in _states.Values)
            SetTargetFade(state, 0f);
    }

    private void EvaluateOcclusion()
    {
        Vector3 camPos = _actionCamera.transform.position;
        List<CinemachineTargetGroup.Target> targets = _targetGroup.Targets;
        IReadOnlyList<ShipOccluderRegistry> registries = _occluderRegistry.Registries;

        for (int r = 0; r < registries.Count; r++)
        {
            ShipOccluderRegistry registry = registries[r];
            if (registry == null) continue;

            IReadOnlyList<Renderer> renderers = registry.OccluderRenderers;
            if (renderers == null) continue;

            for (int i = 0; i < renderers.Count; i++)
            {
                Renderer candidate = renderers[i];
                if (candidate == null) continue;

                bool occludes = IsOccluding(candidate, camPos, targets);
                SetTargetFade(GetOrCreateState(candidate), occludes ? 1f : 0f);
            }
        }
    }

    private static bool IsOccluding(Renderer candidate, Vector3 camPos, List<CinemachineTargetGroup.Target> targets)
    {
        for (int t = 0; t < targets.Count; t++)
        {
            Transform member = targets[t].Object;
            if (member == null) continue;

            Vector3 toMember = member.position - camPos;
            float distanceToTarget = toMember.magnitude;
            if (distanceToTarget <= 0.0001f) continue;

            Ray ray = new Ray(camPos, toMember / distanceToTarget);

            // Only counts as occluding if the hit is genuinely in front of the target member —
            // otherwise geometry behind the subject would be faded too.
            if (candidate.bounds.IntersectRay(ray, out float hitDistance) && hitDistance < distanceToTarget)
                return true;
        }
        return false;
    }

    private RendererFadeState GetOrCreateState(Renderer renderer)
    {
        if (!_states.TryGetValue(renderer, out RendererFadeState state))
        {
            state = new RendererFadeState { Renderer = renderer, Mpb = new MaterialPropertyBlock() };
            _states[renderer] = state;
        }
        return state;
    }

    private void SetTargetFade(RendererFadeState state, float target)
    {
        if (Mathf.Approximately(state.TargetFade, target)) return;

        state.TargetFade = target;
        state.ActiveTween.Stop();
        state.ActiveTween = Tween.Custom(state, state.CurrentFade, target, _fadeDuration,
            static (s, v) => ApplyFade(s, v), Ease.Linear);
    }

    // FadeAmount convention is 0 = visible, 1 = hidden, matching every caller above and the
    // ShipOccluderDither shader's _Fade property (0 = opaque, 1 = fully dithered out), so the
    // value is written straight through. _Fade is a real UnityPerMaterial property, so this
    // MaterialPropertyBlock override is honored per-renderer (unlike the unity_LODFade built-in).
    private static void ApplyFade(RendererFadeState state, float fadeAmount)
    {
        state.CurrentFade = fadeAmount;
        // Now that registries come and go (a destroyed ship, an unloaded scene), _states can hold
        // already-destroyed renderers — even midway through a running tween.
        if (state.Renderer == null) return;

        state.Mpb.SetFloat(FadeId, fadeAmount);
        state.Renderer.SetPropertyBlock(state.Mpb);
    }
}
