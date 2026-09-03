using UnityEngine;

/// <summary>
/// Regola di danno dello scudo elementale: va nei _baseBehaviors di PF_ElementalShield.
///
/// Lo scudo prende danno normale da qualunque elemento (sui suoi HP enormi e' irrilevante ma resta
/// leggibile), mentre un colpo dell'elemento attivo vale sempre una frazione fissa dei suoi HP massimi:
/// e' il modo "giusto" di romperlo e deve saltare all'occhio.
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

        // Ceil e non divisione secca: MaxHp / 3 * 3 resta sotto MaxHp in floating point e il colpo
        // che dovrebbe rompere lo scudo lo lascerebbe vivo con una scheggia di HP.
        payload.Amount = Mathf.Ceil(_controller.MaxHp / Mathf.Max(1, _shield.HitsToBreak));
        return payload;
    }
}
