using UnityEngine;

public abstract class HealthBehaviorSO : ScriptableObject
{
    public virtual void OnEquip(HealthController controller) { }
    public virtual void OnUnequip(HealthController controller) { }
    public virtual DamagePayload ModifyIncomingDamage(DamagePayload payload) => payload;
    public virtual void OnDamageTaken(HealthController controller, DamagePayload payload) { }
    public virtual float ModifyHeal(float amount) => amount;
    public virtual void OnHealReceived(HealthController controller, float amount) { }
    public virtual void OnTurnStart(HealthController controller) { }
    public virtual void OnDeath(HealthController controller) { }
}
