using UnityEngine;

/// <summary>
/// Registra sullo scudo il tipo di ogni colpo diretto all'owner, senza toccare il danno.
/// Lo scudo lo concede all'owner insieme a resistenza e immunita'.
///
/// Serve perche' i colpi annullati dall'immunita' restano l'unico indizio su quali elementi il giocatore
/// ha davvero in mano: il conteggio e' per numero di colpi e non per Amount, cosi' un colpo azzerato pesa
/// quanto gli altri e l'ordine dei behavior nella pipeline di danno diventa irrilevante.
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
