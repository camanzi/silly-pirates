using PrimeTween;
using UnityEngine;

public class ConstellationFragment : MonoBehaviour
{
    [SerializeField] private float _fallDuration = 0.8f;
    [SerializeField] private float _fallHeight = 5f;
    [SerializeField] private int _movementPointsRestored = 2;
    [SerializeField] private WalkingStarPassiveSO _walkingStarPassive;

    private void OnEnable()
    {
        Vector3 target = transform.position;
        transform.position = target + Vector3.up * _fallHeight;
        Tween.Position(transform, target, _fallDuration, Ease.OutCubic);
    }

    private void OnTriggerEnter(Collider other)
    {
        var character = other.GetComponentInParent<GridCharacter>();
        if (character != null)
        {
            character.RemainingMovementPoints += _movementPointsRestored;

            if (_walkingStarPassive != null)
            {
                var passiveController = character.GetComponent<PassiveAbilityController>();
                if (passiveController != null)
                {
                    var instance = Instantiate(_walkingStarPassive);
                    passiveController.AddPassive(instance);
                }
            }
        }

        Destroy(gameObject);
    }
}
