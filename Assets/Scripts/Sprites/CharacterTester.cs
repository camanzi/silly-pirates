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
    [SerializeField] private EnableTacticalViewEventChannel enableTacticalViewEventChannel;
    [SerializeField] private DisableTacticalViewEventChannel disableTacticalViewEventChannel;

    private DirectionalSpriteController _spriteController;
    private bool isPlayingNewAnimation = false;

    private bool _isTacticalViewEnabled = false;

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
        if (_isTacticalViewEnabled)
        {
            disableTacticalViewEventChannel.RaiseEvent();
        } else 
        {
            enableTacticalViewEventChannel.RaiseEvent();
        }
        _isTacticalViewEnabled = !_isTacticalViewEnabled;
    }

    private void OnChangeAnimation(InputAction.CallbackContext ctx) 
    {

        _spriteController.PlayAnimation(isPlayingNewAnimation ? secondAnim : firstAnim);
        isPlayingNewAnimation = !isPlayingNewAnimation;
    }
}
