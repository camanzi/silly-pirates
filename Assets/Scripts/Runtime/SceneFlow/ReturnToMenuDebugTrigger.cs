using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// A TEMPORARY trigger for going back to the menu during combat, pending a real pause or end-of-combat
/// screen. It lives in the combat scene, not in the persistent one, so the key does nothing while
/// already in the menu.
///
/// It reads the keyboard directly instead of going through InputReader: this is scaffolding destined to
/// disappear, and adding an action to GameInput for something that will be removed would leave an
/// orphaned action behind.
/// </summary>
public class ReturnToMenuDebugTrigger : MonoBehaviour
{
    [SerializeField] private VoidEventChannel _returnToMenuRequested;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.escapeKey.wasPressedThisFrame)
            _returnToMenuRequested?.RaiseEvent();
    }
}
