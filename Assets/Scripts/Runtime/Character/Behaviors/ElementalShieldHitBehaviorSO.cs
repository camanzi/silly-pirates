using UnityEngine;

/// <summary>
/// The elemental shield's damage rule: it belongs in PF_ElementalShield's _baseBehaviors.
///
/// The shield takes normal damage from any element (irrelevant against its huge HP pool, but it stays
/// readable), whereas a hit of the active element is always worth a fixed fraction of its maximum HP:
/// that is the "right" way to break it and it has to be obvious.
/// </summary>
[CreateAssetMenu(fileName = "ElementalShieldHitBehavior", menuName = "Combat/Health Behaviors/Elemental Shield Hit")]
public class ElementalShieldHitBehaviorSO : HealthBehaviorSO
{
    private HealthController _controller;
    private ElementalShieldController _shield;

    public override void OnEquip(HealthController controller)
    {
        _controller = controller;
        _shield = controller.GetComponent<ElementalShieldController>();
    }

    public override void OnUnequip(HealthController controller)
    {
        _controller = null;
        _shield = null;
    }

    public override DamagePayload ModifyIncomingDamage(DamagePayload payload)
    {
        if (_shield == null) return payload;

        _shield.RegisterHit(payload.Type);

        if (payload.Type != _shield.ActiveElement) return payload;

        // Ceil and not a plain division: MaxHp / 3 * 3 stays below MaxHp in floating point, and the hit
        // that should break the shield would leave it alive on a sliver of HP.
        payload.Amount = Mathf.Ceil(_controller.MaxHp / Mathf.Max(1, _shield.HitsToBreak));
        return payload;
    }
}
