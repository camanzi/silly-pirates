using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.InputSystem;

public class FreeRoamTarget : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float maxSpeed = 10f;
    [SerializeField] private float acceleration = 5f;
    [SerializeField] private float deceleration = 8f;

    [Header("Rotation Settings")]
    [SerializeField] private bool enableRotation = true;

    [Header("Events channels")]
    [SerializeField] private VoidEventChannel enableTacticalViewEventChannel;
    [SerializeField] private VoidEventChannel disableTacticalViewEventChannel;

    [Header("Anchors")]
    [SerializeField] private MainCameraAnchorSO _mainCameraAnchor;

    [Header("Automoving Camera configs")]
    [SerializeField] private Ease _interpolationCurve;
    
    [Min(0.2f)]
    [SerializeField] private float _interpolationDuration;
    
    [Tooltip("After this timer passed, Camera starts moving automatically on change turn (S)")]
    [Min(1f)]
    [SerializeField] private float _enableAutomovingTimer = 1f;

    private float _lastInputTime = float.NegativeInfinity;

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
            Debug.LogError("Project-Wide Actions non configurate! Vai in Project Settings -> Input System Package.");
        }
    }

    private void OnEnable()
    {
        _moveCameraAction.Enable();
        _tacticalView.Enable();

        _tacticalView.performed += OnToggleTactical;

        if (_mainCameraAnchor == null)
        {
            Debug.LogError($"{nameof(FreeRoamTarget)}: nessun {nameof(MainCameraAnchorSO)} assegnato.", this);
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

        _tacticalView.performed -= OnToggleTactical;
    }

    private void Update()
    {
        HandleInput();
        UpdateMovement();
        UpdateRotation();
        ApplyMovement();
        ApplyRotation();
    }

    public void OnTurnAgentStart(ITurnAgent agent)
    {
        if (agent is not MonoBehaviour mono) return;

        // Se il tempo trascorso in secondi é minore del mio timer NON muovo la telecamera 
        if (Time.time - _lastInputTime < _enableAutomovingTimer) return;

        _cameraMoveTween.Stop();
        _cameraMoveTween = Tween.Position(transform, mono.transform.position, duration: _interpolationDuration, ease: _interpolationCurve);
    }

    #region MOVEMENT & ROTATION
    private void HandleInput()
    {
        
        _moveInputVector = _moveCameraAction.ReadValue<Vector2>();

        if (_moveInputVector.magnitude > 1f)
        {
            _moveInputVector = _moveInputVector.normalized;
            _lastInputTime = Time.time;
        }
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
            _currentVelocity = Vector3.MoveTowards(_currentVelocity, Vector3.zero, deceleration * Time.deltaTime);
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
    private void OnToggleTactical(InputAction.CallbackContext ctx)
    {
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
