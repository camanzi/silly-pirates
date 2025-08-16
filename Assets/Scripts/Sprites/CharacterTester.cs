using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterTester : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private InputActionAsset _playerInput;
    
    [Header("Animation testing")]
    [SerializeField] private string firstAnim;
    [SerializeField] private string secondAnim;

    private DirectionalSpriteController _spriteController;
    private bool isPlayingNewAnimation = false;

    private InputAction _changeAnimation;

    private void Awake()
    {
        _spriteController = GetComponent<DirectionalSpriteController>();
        InputActionAsset inputActions = InputSystem.actions;
        if (inputActions != null)
        {
            _changeAnimation = inputActions.FindAction("ChangeAnimation");
        }
        else
        {
            Debug.LogError("Project-Wide Actions non configurate! Vai in Project Settings -> Input System Package.");
        }
    }

    private void OnEnable()
    {
        if (_changeAnimation != null)
            _changeAnimation.performed += OnChangeAnimation;
    }

    private void OnDisable()
    {
        if (_changeAnimation != null)
            _changeAnimation.performed -= OnChangeAnimation;
    }

    private void OnChangeAnimation(InputAction.CallbackContext ctx) 
    {
        _spriteController.PlayAnimation(isPlayingNewAnimation ? secondAnim : firstAnim);
        isPlayingNewAnimation = !isPlayingNewAnimation;
    }
}
