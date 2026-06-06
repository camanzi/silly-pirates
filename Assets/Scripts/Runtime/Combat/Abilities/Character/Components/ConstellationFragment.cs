using PrimeTween;
using UnityEngine;

public class ConstellationFragment : MonoBehaviour
{
    [SerializeField] private float _fallDuration = 0.8f;
    [SerializeField] private float _fallHeight = 5f;
    [SerializeField] private int _movementPointsRestored = 2;
    [SerializeField] private PassiveAbilitySO _passiveOnPickup;

    private void OnEnable()
    {
        Vector3 target = transform.position;
        transform.position = target + Vector3.up * _fallHeight;
        Tween.Position(transform, target, _fallDuration, Ease.OutCubic);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<GridCharacter>(out GridCharacter character))
        {
            character.RemainingMovementPoints += _movementPointsRestored;

            if (_passiveOnPickup != null)
            {
                var passiveController = character.GetComponent<PassiveAbilityController>();
                if (passiveController != null)
                {
                    var instance = Instantiate(_passiveOnPickup);
                    passiveController.AddPassive(instance);
                }
            }
        }

        Destroy(gameObject);
    }
}
