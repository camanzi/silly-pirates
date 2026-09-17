using UnityEngine;

/// <summary>
/// Records on the shield the type of every hit aimed at the owner, without touching the damage.
/// The shield grants it to the owner alongside resistance and immunity.
///
/// It exists because the hits nullified by immunity remain the only clue as to which elements the player
/// actually has in hand: the count is by number of hits and not by Amount, so a nullified hit weighs as
/// much as any other and the ordering of behaviors in the damage pipeline becomes irrelevant.
/// </summary>
[CreateAssetMenu(fileName = "ElementalShieldTrackerBehavior", menuName = "Combat/Health Behaviors/Elemental Shield Tracker")]
public class ElementalShieldTrackerBehaviorSO : HealthBehaviorSO
{
    private ElementalShieldController _shield;

    public void Bind(ElementalShieldController shield) => _shield = shield;

    public override void OnUnequip(HealthController controller) => _shield = null;

    public override DamagePayload ModifyIncomingDamage(DamagePayload payload)
    {
        _shield?.RegisterHit(payload.Type);
        return payload;
    }
}
