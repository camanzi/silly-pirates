using UnityEngine;

/// <summary>
/// Nullifies an element's damage entirely. The twin of <see cref="ResistanceBehaviorSO"/> and
/// <see cref="VulnerabilityBehaviorSO"/>: the same constant multiplier, only at zero.
/// </summary>
[CreateAssetMenu(fileName = "ImmunityBehavior", menuName = "Combat/Health Behaviors/Immunity")]
public class ImmunityBehaviorSO : HealthBehaviorSO
{
    private const float Multiplier = 0f;

    [SerializeField] private DamageType _immuneTo;

    public DamageType ImmuneTo => _immuneTo;

    public override DamagePayload ModifyIncomingDamage(DamagePayload payload)
    {
        if (payload.Type == _immuneTo)
            payload.Amount *= Multiplier;
        return payload;
    }
}
