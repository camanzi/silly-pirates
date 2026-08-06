using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class CameraDirector : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private CinemachineCamera _actionCamera;
    [SerializeField] private CinemachineTargetGroup _targetGroup;
    [SerializeField] private CinemachineBrain _brain;
    [SerializeField] private CameraDirectorStateSO _directorState;
    [SerializeField] private AbilityExecutionCueEventChannel _cueChannel;
    [SerializeField] private CameraCueDefaultProfilesSO _cueDefaults;

    [Tooltip("Anchor moved onto ground-targeted positions so the target group can frame abilities with no ITargettable")]
    [SerializeField] private Transform _groundAnchor;

    [Tooltip("Safety cap on how long a cue waits for the Brain blend before signaling ready")]
    [Min(0.5f)]
    [SerializeField] private float _maxBlendWait = 2f;

    private CinemachineGroupFraming _groupFraming;
    private CameraCueProfileSO _activeProfile;
    private bool _isCueActive;

    // Pool of ground anchors, grown on demand so a cue can frame several areas at once
    // (one member per affected point) rather than a single centroid.
    private readonly List<Transform> _groundAnchors = new();
    private Func<int, Transform> _groundAnchorProvider;

    private void Awake()
    {
        if (_actionCamera != null) _groupFraming = _actionCamera.GetComponent<CinemachineGroupFraming>();
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
    }

    private void OnDisable()
    {
        if (_directorState != null) _directorState.OnFocusEnded -= OnFocusEnded;
        if (_cueChannel != null) _cueChannel.OnEventRaised -= HandleCue;
    }

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
        _activeProfile = context.Profile;
        _isCueActive = true;

        if (_groupFraming != null) _groupFraming.FramingSize = context.Profile.FramingSize;
        _actionCamera.enabled = true;

        await handler.RunAsync(context);

        if (_directorState != null) _directorState.SignalFocusReady();
    }

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

        _actionCamera.enabled = false;
        _activeProfile = null;
    }
}
