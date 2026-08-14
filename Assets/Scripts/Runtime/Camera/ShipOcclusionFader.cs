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

    private void OnEnable()
    {
        // Pull-then-subscribe, come CameraDirector: il rig può esistere già o arrivare dopo.
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
    /// Porta ogni renderer candidato a "pienamente visibile" invece di lasciarlo indefinito fino
    /// alla prima valutazione di occlusione: SetTargetFade uscirebbe subito per uno stato appena
    /// creato il cui target coincide già con lo 0f di default, senza mai scrivere un
    /// MaterialPropertyBlock. Chiamato anche per i registry che si registrano più tardi (nave che
    /// arriva nell'intro, scena caricata in additivo).
    /// </summary>
    private void InitializeRenderers(ShipOccluderRegistry registry)
    {
        if (registry == null) return;

        IReadOnlyList<Renderer> renderers = registry.OccluderRenderers;
        if (renderers == null) return;

        for (int i = 0; i < renderers.Count; i++)
        {
            Renderer candidate = renderers[i];
            if (candidate == null) continue;

            ApplyFade(GetOrCreateState(candidate), 0f);
        }
    }

    private void Update()
    {
        if (_actionCamera == null || !_actionCamera.enabled)
        {
            FadeAllOut();
            return;
        }

        if (_targetGroup == null || _occluderRegistry == null) return;

        EvaluateOcclusion();
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
        // Ora che i registry vanno e vengono (nave distrutta, scena scaricata), _states può
        // contenere renderer già distrutti — anche a metà di un tween in corso.
        if (state.Renderer == null) return;

        state.Mpb.SetFloat(FadeId, fadeAmount);
        state.Renderer.SetPropertyBlock(state.Mpb);
    }
}
