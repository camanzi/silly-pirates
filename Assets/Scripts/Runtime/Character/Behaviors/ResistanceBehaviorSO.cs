using UnityEngine;

[CreateAssetMenu(fileName = "ResistanceBehavior", menuName = "Combat/Health Behaviors/Resistance")]
public class ResistanceBehaviorSO : HealthBehaviorSO
{
    private const float Multiplier = 0.5f;

    [SerializeField] private DamageType _resistantTo;

    public override DamagePayload ModifyIncomingDamage(DamagePayload payload)
    {
        if (payload.Type == _resistantTo)
            payload.Amount *= Multiplier;
        return payload;
    }
}
