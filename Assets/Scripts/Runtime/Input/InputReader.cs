using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "Input/InputReader")]
public class InputReader : ScriptableObject, GameInput.IUIActions, GameInput.IPlayerActions
{
    public event UnityAction<Vector2> PointEvent = delegate { };
    public event UnityAction ClickStartedEvent = delegate { };
    public event UnityAction RightClickEvent = delegate { };

    /// <summary>Raised by the Player/Pause action (Escape, gamepad Start). The pause menu owns the
    /// decision of what it means: this reader only reports the press.</summary>
    public event UnityAction PauseEvent = delegate { };

    private GameInput _gameInput;

    private void OnEnable()
    {
        if (_gameInput == null)
        {
            _gameInput = new GameInput();
            
            _gameInput.UI.SetCallbacks(this);
            _gameInput.Player.SetCallbacks(this);
        }
        
        _gameInput.UI.Enable();
        _gameInput.Player.Enable();
    }

    private void OnDisable()
    {
        _gameInput.UI.Disable();
        _gameInput.Player.Disable();
    }

    // --- IUIActions implementation ---
    
    public void OnPoint(InputAction.CallbackContext context)
    {
        PointEvent?.Invoke(context.ReadValue<Vector2>());
    }

    public void OnClick(InputAction.CallbackContext context)
    {
        if (context.performed && context.ReadValueAsButton()) ClickStartedEvent?.Invoke();
    }

    public void OnRightClick(InputAction.CallbackContext context)
    {
        if (context.performed && context.ReadValueAsButton()) RightClickEvent?.Invoke();
    }

    // UI interface methods we are required to declare
    public void OnNavigate(InputAction.CallbackContext context) { }
    public void OnSubmit(InputAction.CallbackContext context) { }
    public void OnCancel(InputAction.CallbackContext context) { }
    public void OnMiddleClick(InputAction.CallbackContext context) { }
    public void OnScrollWheel(InputAction.CallbackContext context) { }
    public void OnTrackedDevicePosition(InputAction.CallbackContext context) { }
    public void OnTrackedDeviceOrientation(InputAction.CallbackContext context) { }

    // --- IPlayerActions implementation (for completeness) ---

    public void OnMoveCamera(InputAction.CallbackContext context) { }
    public void OnTacticalView(InputAction.CallbackContext context) { }

    public void OnPause(InputAction.CallbackContext context)
    {
        // 'performed' only: the generated wrapper also forwards started/canceled, which would make a
        // single key press toggle the pause three times.
        if (context.performed) PauseEvent?.Invoke();
    }
}
