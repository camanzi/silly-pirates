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
    [SerializeField] private EnableTacticalViewEventChannel enableTacticalViewEventChannel;
    [SerializeField] private DisableTacticalViewEventChannel disableTacticalViewEventChannel;

    private Transform _cameraTransform;
    
    private Vector2 _moveInputVector;

    private Vector3 _currentVelocity;
    private Vector3 _targetVelocity;

    private Quaternion targetRotation;

    private InputAction _moveCameraAction;
    private InputAction _tacticalView;

    private bool _isTacticalViewActive;

    private void Awake()
    {
        _cameraTransform = Camera.main.transform;

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
    }

    private void OnDisable()
    {
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
    #region Movement & Rotation
    private void HandleInput()
    {
        
        _moveInputVector = _moveCameraAction.ReadValue<Vector2>();

        if (_moveInputVector.magnitude > 1f)
        {
            _moveInputVector = _moveInputVector.normalized;
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
        if (!enableRotation) return;

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
