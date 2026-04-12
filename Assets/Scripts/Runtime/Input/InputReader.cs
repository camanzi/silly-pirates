using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "Input/InputReader")]
public class InputReader : ScriptableObject, GameInput.IUIActions, GameInput.IPlayerActions
{
    public event UnityAction<Vector2> PointEvent = delegate { };
    public event UnityAction ClickStartedEvent = delegate { };
    public event UnityAction RightClickEvent = delegate { };

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

    // --- Implementazione IUIActions ---
    
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

    // Metodi dell'interfaccia UI che dobbiamo dichiarare
    public void OnNavigate(InputAction.CallbackContext context) { }
    public void OnSubmit(InputAction.CallbackContext context) { }
    public void OnCancel(InputAction.CallbackContext context) { }
    public void OnMiddleClick(InputAction.CallbackContext context) { }
    public void OnScrollWheel(InputAction.CallbackContext context) { }
    public void OnTrackedDevicePosition(InputAction.CallbackContext context) { }
    public void OnTrackedDeviceOrientation(InputAction.CallbackContext context) { }

    // --- Implementazione IPlayerActions (Per completezza) ---

    public void OnMoveCamera(InputAction.CallbackContext context) { }
    public void OnTacticalView(InputAction.CallbackContext context) { }
}
