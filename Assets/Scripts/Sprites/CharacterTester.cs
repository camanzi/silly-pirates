using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterTester : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private InputActionAsset _playerInput;
    
    [Header("Animation testing")]
    [SerializeField] private string firstAnim;
    [SerializeField] private string secondAnim;

    [Header("Events channels")]
    [SerializeField] private EnableTacticalViewEventChannel tacticalViewEventChannel;

    private DirectionalSpriteController _spriteController;
    private bool isPlayingNewAnimation = false;

    private void Awake()
    {
        _spriteController = GetComponent<DirectionalSpriteController>();
    }

    private void OnEnable()
    {
        _playerInput.FindActionMap("Test").Enable();
        _playerInput.FindActionMap("Test").FindAction("ToggleTactical").performed += OnToggleTactical;
        _playerInput.FindActionMap("Test").FindAction("ChangeAnimation").performed += OnChangeAnimation;
    }

    private void OnToggleTactical(InputAction.CallbackContext ctx) 
    {
        tacticalViewEventChannel.RaiseEvent();
    }

    private void OnChangeAnimation(InputAction.CallbackContext ctx) 
    {

        _spriteController.PlayAnimation(isPlayingNewAnimation ? secondAnim : firstAnim);
        isPlayingNewAnimation = !isPlayingNewAnimation;
    }
}
