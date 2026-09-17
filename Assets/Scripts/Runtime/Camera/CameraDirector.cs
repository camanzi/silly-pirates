using System;
using System.Collections.Generic;
using PrimeTween;
using Unity.Cinemachine;
using UnityEngine;

public class CameraDirector : MonoBehaviour
{
    // The camera rig lives in other prefabs and so cannot be referenced from here: it arrives through
    // anchor SOs, resolved with the pull-then-subscribe contract documented on RuntimeAnchorSO.
    [Header("Camera Rig (anchors)")]
    [SerializeField] private CinemachineCameraAnchorSO _actionCameraAnchor;
    [SerializeField] private CinemachineTargetGroupAnchorSO _targetGroupAnchor;
    [SerializeField] private CinemachineBrainAnchorSO _brainAnchor;

    [Header("Dependencies")]
    [SerializeField] private CameraDirectorStateSO _directorState;
    [SerializeField] private AbilityExecutionCueEventChannel _cueChannel;
    [SerializeField] private CameraCueDefaultProfilesSO _cueDefaults;

    [Tooltip("Anchor moved onto ground-targeted positions so the target group can frame abilities with no ITargettable")]
    [SerializeField] private Transform _groundAnchor;

    [Tooltip("Safety cap on how long a cue waits for the Brain blend before signaling ready")]
    [Min(0.5f)]
    [SerializeField] private float _maxBlendWait = 2f;

    private CinemachineCamera _actionCamera;
    private CinemachineTargetGroup _targetGroup;
    private CinemachineBrain _brain;

    private CinemachineGroupFraming _groupFraming;
    private CameraCueProfileSO _activeProfile;
    private bool _isCueActive;

    // Orbit drift. The action camera has no Aim stage, so its Transform rotation IS the vcam state
    // orientation: yawing it makes CinemachinePositionComposer re-anchor the camera around the tracked
    // point, i.e. a real orbit around the framed pivot. _baseRotation is captured once so repeated cues
    // can never accumulate drift.
    private Tween _orbitTween;
    private Quaternion _baseCameraRotation;

    // Pool of ground anchors, grown on demand so a cue can frame several areas at once
    // (one member per affected point) rather than a single centroid.
    private readonly List<Transform> _groundAnchors = new();
    private Func<int, Transform> _groundAnchorProvider;

    private void Awake()
    {
        if (_groundAnchor == null)
        {
            _groundAnchor = new GameObject("CueGroundAnchor").transform;
            _groundAnchor.SetParent(transform);
        }
        _groundAnchors.Add(_groundAnchor);
        _groundAnchorProvider = GetGroundAnchor;
    }

    private Transform GetGroundAnchor(int index)
    {
        while (_groundAnchors.Count <= index)
        {
            var anchor = new GameObject($"CueGroundAnchor_{_groundAnchors.Count}").transform;
            anchor.SetParent(transform);
            _groundAnchors.Add(anchor);
        }
        return _groundAnchors[index];
    }

    private void OnEnable()
    {
        if (_directorState != null) _directorState.OnFocusEnded += OnFocusEnded;
        if (_cueChannel != null) _cueChannel.OnEventRaised += HandleCue;

        // Pull-then-subscribe: the pull picks up the rig already in the scene, the subscribe picks up the
        // one arriving later (director in the persistent scene, rig in the additive combat one).
        if (_actionCameraAnchor != null)
        {
            HandleActionCameraChanged(_actionCameraAnchor.Value);
            _actionCameraAnchor.OnValueChanged += HandleActionCameraChanged;
        }
        if (_targetGroupAnchor != null)
        {
            _targetGroup = _targetGroupAnchor.Value;
            _targetGroupAnchor.OnValueChanged += HandleTargetGroupChanged;
        }
        if (_brainAnchor != null)
        {
            _brain = _brainAnchor.Value;
            _brainAnchor.OnValueChanged += HandleBrainChanged;
        }
    }

    private void OnDisable()
    {
        if (_directorState != null) _directorState.OnFocusEnded -= OnFocusEnded;
        if (_cueChannel != null) _cueChannel.OnEventRaised -= HandleCue;

        if (_actionCameraAnchor != null) _actionCameraAnchor.OnValueChanged -= HandleActionCameraChanged;
        if (_targetGroupAnchor != null) _targetGroupAnchor.OnValueChanged -= HandleTargetGroupChanged;
        if (_brainAnchor != null) _brainAnchor.OnValueChanged -= HandleBrainChanged;

        StopOrbit();
    }

    private void HandleActionCameraChanged(CinemachineCamera camera)
    {
        // The orbit tween drives the previous camera's transform: it has to be stopped before that
        // reference is lost, or it keeps rotating an object we no longer govern.
        _orbitTween.Stop();

        _actionCamera = camera;
        if (camera == null)
        {
            _groupFraming = null;
            return;
        }

        // Captured here — not in Awake, and not "on the first cue behind a one-shot flag". The session's
        // second combat brings a NEW action camera instance: a one-shot capture would use the rotation of
        // the previous, now destroyed one as its base. At this point the transform is still the authored
        // one, because no cue can start before the rig has registered itself.
        _groupFraming = camera.GetComponent<CinemachineGroupFraming>();
        _baseCameraRotation = camera.transform.rotation;
    }

    private void HandleTargetGroupChanged(CinemachineTargetGroup targetGroup) => _targetGroup = targetGroup;

    private void HandleBrainChanged(CinemachineBrain brain) => _brain = brain;

    public void HandleCue(AbilityExecutionCue cue)
    {
        CameraCueType cueType = cue.CueTypeOverride ?? (cue.Ability != null ? cue.Ability.CameraCue : CameraCueType.None);
        CameraCueProfileSO profile = cue.ProfileOverride != null
            ? cue.ProfileOverride
            : cue.Ability != null && cue.Ability.CameraCueProfile != null
                ? cue.Ability.CameraCueProfile
                : _cueDefaults != null ? _cueDefaults.GetProfile(cueType) : null;

        ICameraCueHandler handler = CameraCueHandlerFactory.GetHandler(cueType);

        if (handler == null || profile == null || _actionCamera == null || _targetGroup == null)
        {
            if (_directorState != null) _directorState.SignalFocusReady();
            return;
        }

        var context = new CameraCueContext {
            TargetGroup = _targetGroup,
            GroupFraming = _groupFraming,
            Brain = _brain,
            GroundAnchorProvider = _groundAnchorProvider,
            Cue = cue,
            Profile = profile,
            MaxBlendWait = _maxBlendWait
        };

        RunCueAsync(handler, context);
    }

    private async void RunCueAsync(ICameraCueHandler handler, CameraCueContext context)
    {
        // Back-to-back cues (intro beats) re-enter here without an intervening release, so the previous
        // sweep must die and the yaw be re-seeded BEFORE the reframe — the jump then hides inside the
        // repositioning caused by the group being cleared and repopulated.
        SeedOrbitStart(context.Profile);

        _activeProfile = context.Profile;
        _isCueActive = true;

        if (_groupFraming != null) _groupFraming.FramingSize = context.Profile.FramingSize;
        _actionCamera.enabled = true;

        await handler.RunAsync(context);

        // Fire-and-forget: the shot keeps drifting while the caller proceeds with the ability/spawn.
        StartOrbit(context.Profile);

        if (_directorState != null) _directorState.SignalFocusReady();
    }

    /// <summary>
    /// Kills any running sweep and parks the vcam at the orbit's starting yaw (-half the sweep), so the
    /// shot blends in already rotated and the drift crosses the authored angle instead of leaving it.
    /// Cues without an orbit are parked back on the authored angle.
    /// </summary>
    private void SeedOrbitStart(CameraCueProfileSO profile)
    {
        _orbitTween.Stop();
        if (_actionCamera == null) return;

        bool hasOrbit = profile != null && profile.HasOrbit;
        _actionCamera.transform.rotation = OrbitRotation(hasOrbit ? -HalfSweep(profile) : 0f);
    }

    private void StartOrbit(CameraCueProfileSO profile)
    {
        if (profile == null || !profile.HasOrbit || _actionCamera == null) return;

        float half = HalfSweep(profile);
        float duration = profile.OrbitMaxDegrees / Mathf.Abs(profile.OrbitSpeed);

        _orbitTween = Tween.Rotation(_actionCamera.transform,
            startValue: OrbitRotation(-half),
            endValue: OrbitRotation(half),
            duration, profile.OrbitEase);
    }

    private void StopOrbit()
    {
        _orbitTween.Stop();
        if (_actionCamera != null) _actionCamera.transform.rotation = _baseCameraRotation;
    }

    // Signed: OrbitSpeed's sign is the sweep direction, OrbitMaxDegrees its total travel.
    private static float HalfSweep(CameraCueProfileSO profile)
        => Mathf.Sign(profile.OrbitSpeed) * profile.OrbitMaxDegrees * 0.5f;

    private Quaternion OrbitRotation(float degrees)
        => Quaternion.AngleAxis(degrees, Vector3.up) * _baseCameraRotation;

    private void OnFocusEnded()
    {
        if (!_isCueActive) return;
        _isCueActive = false;
        ReleaseAsync();
    }

    private async void ReleaseAsync()
    {
        float hold = _activeProfile != null ? _activeProfile.PostShotHold : 0f;
        if (hold > 0f)
            await Awaitable.WaitForSecondsAsync(hold);

        // A new cue may have started during the hold — don't steal its camera
        if (_isCueActive) return;

        // Never hand the vcam back to the player rotated
        StopOrbit();
        // The rig may have gone away during the hold (a scene unload): StopOrbit tolerates that already,
        // this does not.
        if (_actionCamera != null) _actionCamera.enabled = false;
        _activeProfile = null;
    }
}
