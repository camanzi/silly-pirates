using UnityEngine;

/// <summary>
/// Annulla completamente il danno di un elemento. Gemello di <see cref="ResistanceBehaviorSO"/> e
/// <see cref="VulnerabilityBehaviorSO"/>: stesso moltiplicatore costante, solo a zero.
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
