using UnityEngine;

public class AnimatorTester : MonoBehaviour
{
    [Header("Configs")]
    [SerializeField] private string firstAnim;
    [SerializeField] private string secondAnim;

    private DirectionalSpriteController _spriteController;
    private bool isPlayingNewAnimation = false;

    private void Awake()
    {
        _spriteController = GetComponent<DirectionalSpriteController>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) 
        {
            _spriteController.PlayAnimation(isPlayingNewAnimation ? secondAnim : firstAnim);
            isPlayingNewAnimation = !isPlayingNewAnimation;
        }
    }
}
