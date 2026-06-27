using UnityEngine;

[CreateAssetMenu(fileName = "Accuracy Overcap Passive", menuName = "Abilities/Equipment/Accuracy Overcap Passive")]
public class AccuracyOvercapPassiveSO : PassiveAbilitySO, IHitRateModifier
{
    private int _hitRateBonus;
    private PassiveAbilityController _controller;

    public void Initialize(int bonus) => _hitRateBonus = bonus;

    int IHitRateModifier.GetHitRateBonus() => _hitRateBonus;

    public override void OnEquip(PassiveAbilityController controller)
    {
        _controller = controller;
        if (controller.TryGetComponent<OffensiveEquipment>(out var eq))
            eq.AddHitRateModifier(this);
        if (controller.TryGetComponent<ShipEquipment>(out var ship))
            ship.OnCommandExecuted.AddListener(SelfRemove);
    }

    public override void OnUnequip(PassiveAbilityController controller)
    {
        if (controller.TryGetComponent<OffensiveEquipment>(out var eq))
            eq.RemoveHitRateModifier(this);
        if (controller.TryGetComponent<ShipEquipment>(out var ship))
            ship.OnCommandExecuted.RemoveListener(SelfRemove);
        _controller = null;
    }

    private void SelfRemove() => _controller?.RemovePassive(this);
}
