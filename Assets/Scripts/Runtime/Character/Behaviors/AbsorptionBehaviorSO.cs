using UnityEngine;

/// <summary>
/// Turns an element's damage into healing. The twin of <see cref="ResistanceBehaviorSO"/>,
/// <see cref="ImmunityBehaviorSO"/> and <see cref="VulnerabilityBehaviorSO"/>: the same constant
/// multiplier, negative this time — <see cref="HealthController.ApplyDamage"/> already reroutes a negative
/// Amount to ApplyHeal on its own, so no extra code is needed to heal.
///
/// A composition trap: the four behaviors filter on different <see cref="DamageType"/> values and never
/// add up with each other, but a Resistance on the same element in the character's _baseBehaviors would
/// compose with this one (x0.5 then x-1 = half healing). It does not happen today — the targets using this
/// behavior have empty _baseBehaviors — but it is worth keeping in mind when adding new ones.
/// </summary>
[CreateAssetMenu(fileName = "AbsorptionBehavior", menuName = "Combat/Health Behaviors/Absorption")]
public class AbsorptionBehaviorSO : HealthBehaviorSO
{
    private const float Multiplier = -1f;

    [SerializeField] private DamageType _absorbs;

    public DamageType Absorbs => _absorbs;

    public override DamagePayload ModifyIncomingDamage(DamagePayload payload)
    {
        if (payload.Type == _absorbs)
            payload.Amount *= Multiplier;
        return payload;
    }
}
