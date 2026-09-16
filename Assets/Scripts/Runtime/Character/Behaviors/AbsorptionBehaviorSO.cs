using UnityEngine;

/// <summary>
/// Trasforma il danno di un elemento in cura. Gemello di <see cref="ResistanceBehaviorSO"/>,
/// <see cref="ImmunityBehaviorSO"/> e <see cref="VulnerabilityBehaviorSO"/>: stesso moltiplicatore
/// costante, questa volta negativo — <see cref="HealthController.ApplyDamage"/> dirotta gia' da solo
/// un Amount negativo su ApplyHeal, quindi non serve altro codice per curare.
///
/// Trappola di composizione: i quattro behavior filtrano su <see cref="DamageType"/> diversi e non si
/// sommano mai fra loro, ma una Resistance sullo stesso elemento nei _baseBehaviors del personaggio
/// comporrebbe con questa (x0.5 poi x-1 = mezza cura). Oggi non succede — i bersagli che usano questo
/// behavior hanno _baseBehaviors vuoti — ma va tenuto a mente aggiungendone di nuovi.
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
