using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.InputSystem;

public class FreeRoamTarget : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float acceleration = 5f;

    [Header("Rotation Settings")]
    [SerializeField] private bool enableRotation = true;

    [Header("Events channels")]
    [SerializeField] private VoidEventChannel enableTacticalViewEventChannel;
    [SerializeField] private VoidEventChannel disableTacticalViewEventChannel;

    [Header("Anchors")]
    [SerializeField] private MainCameraAnchorSO _mainCameraAnchor;

    [Header("Pause")]
    [Tooltip("Optional: while paused the camera stops reading input. Leave empty in scenes with no pause menu")]
    [SerializeField] private PauseStateSO _pauseState;

    [Header("Cinematics")]
    [Tooltip("Optional: while the director is framing an ability the camera stops reading input, so the " +
             "target cannot be dragged away behind a shot the player is not looking through. Leave empty in " +
             "scenes with no camera direction")]
    [SerializeField] private CameraDirectorStateSO _directorState;

    [Header("Turn")]
    [Tooltip("Optional: while a non-player agent holds the turn the target cannot be panned. Leave empty " +
             "in scenes with no turn system")]
    [SerializeField] private TurnStateSO _turnState;

    [Header("Automoving Camera configs")]
    [SerializeField] private Ease _interpolationCurve;
    
    [Min(0.2f)]
    [SerializeField] private float _interpolationDuration;
    
    [Tooltip("The turn-start recentering is skipped when the target is already this close (world units, " +
             "Y ignored) to the activating agent. This is the anti-nausea guard: an agent with several " +
             "actions per turn re-raises the activation event for each one, and without it the camera would " +
             "tween back and forth between them")]
    [Min(0f)]
    [SerializeField] private float _recenterDeadzone = 1.5f;

    private Transform _cameraTransform;
    
    private Vector2 _moveInputVector;

    private Vector3 _currentVelocity;
    private Vector3 _targetVelocity;

    private Quaternion targetRotation;

    private InputAction _moveCameraAction;
    private InputAction _tacticalView;

    private bool _isTacticalViewActive;
    private Tween _cameraMoveTween;

    private void Awake()
    {
        InputActionAsset inputActions = InputSystem.actions;
        if (inputActions != null)
        {
            _moveCameraAction = inputActions.FindAction("MoveCamera");
            _tacticalView = inputActions.FindAction("TacticalView");
        }
        else
        {
            Debug.LogError("Project-Wide Actions not configured! Go to Project Settings -> Input System Package.");
        }
    }

    private void OnEnable()
    {
        _moveCameraAction.Enable();
        _tacticalView.Enable();

        _tacticalView.performed += OnToggleTactical;

        if (_mainCameraAnchor == null)
        {
            Debug.LogError($"{nameof(FreeRoamTarget)}: no {nameof(MainCameraAnchorSO)} assigned.", this);
            return;
        }

        HandleCameraChanged(_mainCameraAnchor.Value);
        _mainCameraAnchor.OnValueChanged += HandleCameraChanged;
    }

    private void HandleCameraChanged(Camera camera)
        => _cameraTransform = camera != null ? camera.transform : null;

    private void OnDisable()
    {
        if (_mainCameraAnchor != null) _mainCameraAnchor.OnValueChanged -= HandleCameraChanged;

        _moveCameraAction.Disable();
        _tacticalView.Disable();

        _tacticalView.performed -= OnToggleTactical;
    }

    private void Update()
    {
        // The pan is already still at timeScale 0 (it is driven by Time.deltaTime), but the input would
        // keep being read and accumulated: on resume the camera would jump. The velocity is dropped rather
        // than frozen for the same reason: whatever was built up on the last live frame must not be picked
        // back up when control returns.
        if (IsPanSuspended)
        {
            _currentVelocity = Vector3.zero;
            return;
        }

        HandleInput();
        UpdateMovement();
        UpdateRotation();
        ApplyMovement();
        ApplyRotation();
    }

    public void OnTurnAgentStart(ITurnAgent agent)
    {
        if (agent is not MonoBehaviour mono) return;

        // Already on the agent: nothing to do. This is what keeps an agent with several actions per turn from
        // making the camera bounce — TurnController raises the activation event once per action index, not
        // once per turn. A character that MOVED between two abilities is past the deadzone, so it is still
        // followed.
        if (IsWithinDeadzone(mono.transform.position)) return;

        MoveTo(mono.transform);
    }

    // Planar: the target rides at y = 1 while the agents stand on the floor, so a 3D distance would never
    // fall inside the deadzone.
    private bool IsWithinDeadzone(Vector3 position)
    {
        Vector2 delta = new(position.x - transform.position.x, position.z - transform.position.z);
        return delta.sqrMagnitude <= _recenterDeadzone * _recenterDeadzone;
    }

    /// <summary>
    /// Centers on the agent unconditionally: unlike the automatic turn-start move, an explicit click always
    /// wins, so it is not subject to the deadzone.
    /// </summary>
    public void FocusOnAgent(ITurnAgent agent)
    {
        if (agent is not MonoBehaviour mono) return;

        MoveTo(mono.transform);
    }

    private void MoveTo(Transform target)
    {
        _cameraMoveTween.Stop();
        _cameraMoveTween = Tween.Position(transform, target.position, duration: _interpolationDuration, ease: _interpolationCurve);
    }

    #region MOVEMENT & ROTATION
    private void HandleInput()
    {
        
        _moveInputVector = _moveCameraAction.ReadValue<Vector2>();

        if (_moveInputVector.magnitude > 1f)
            _moveInputVector = _moveInputVector.normalized;
    }

    private void UpdateMovement()
    {
        Vector3 inputDirection = new Vector3(_moveInputVector.x, 0, _moveInputVector.y);
        Vector3 worldInputDirection = transform.TransformDirection(inputDirection);
        _targetVelocity = worldInputDirection * maxSpeed;

        if (inputDirection.magnitude > 0.1f)
        {
            float lerpSpeed = acceleration * Time.deltaTime;

            float directionChange = Vector3.Dot(_currentVelocity.normalized, inputDirection);
            if (directionChange < 0.5f && _currentVelocity.magnitude > 0.1f)
            {
                lerpSpeed *= 2f;
            }

            _currentVelocity = Vector3.Lerp(_currentVelocity, _targetVelocity, lerpSpeed);
        }
        else
        {
            _currentVelocity = Vector3.zero;
        }
    }

    private void UpdateRotation()
    {
        if (!enableRotation || _cameraTransform == null) return;

        Vector3 cameraForward = _cameraTransform.forward;
        cameraForward.y = 0;
        Vector3 directionToRotateTowards = cameraForward.normalized;
       
        if (directionToRotateTowards.magnitude > 0.1f)
        {
            targetRotation = Quaternion.LookRotation(directionToRotateTowards);
        }
    }

    private void ApplyMovement()
    {
        Vector3 deltaMovement = _currentVelocity * Time.deltaTime;
        transform.position += deltaMovement;
    }

    private void ApplyRotation()
    {
        if (!enableRotation) return;

        transform.rotation = targetRotation;
    }
    #endregion
    private bool IsPaused => _pauseState != null && _pauseState.IsPaused;

    private bool IsCinematic => _directorState != null && _directorState.IsCinematicActive;

    private bool IsEnemyTurn => _turnState != null && _turnState.IsEnemyTurn;

    /// <summary>
    /// Every camera input. A pause or a cue means the player is not meant to be driving anything.
    /// </summary>
    private bool IsInputSuspended => IsPaused || IsCinematic;

    /// <summary>
    /// The pan only, which is suspended in one more case than the rest: an enemy's turn. The two levels are
    /// deliberately not the same — most of an enemy's turn is dead time (a second of delay, then the behavior
    /// tree), and the tactical view is all the player has to read the board while waiting. Moving the target
    /// under the AI is what has to stop, not looking around.
    /// </summary>
    private bool IsPanSuspended => IsInputSuspended || IsEnemyTurn;

    private void OnToggleTactical(InputAction.CallbackContext ctx)
    {
        // The action stays enabled while suspended: the callback still fires, so it is filtered here.
        if (IsInputSuspended) return;

        if (_isTacticalViewActive)
        {
            disableTacticalViewEventChannel.RaiseEvent();
        }
        else
        {
            enableTacticalViewEventChannel.RaiseEvent();
        }
        _isTacticalViewActive = !_isTacticalViewActive;
    }
}
