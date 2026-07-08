using UnityEngine;

[CreateAssetMenu(fileName = "Accuracy Overcap Passive", menuName = "Abilities/Equipment/Accuracy Overcap Passive")]
public class AccuracyOvercapPassiveSO : PassiveAbilitySO, IAccuracyModifier, IOvercapPassive
{
    private int _accuracyBonus;
    private PassiveAbilityController _controller;

    public void Initialize(int bonus) => _accuracyBonus = bonus;

    int IAccuracyModifier.GetAccuracyBonus() => _accuracyBonus;

    public override void OnEquip(PassiveAbilityController controller)
    {
        _controller = controller;
        if (controller.TryGetComponent<OffensiveEquipment>(out var eq))
            eq.AddAccuracyModifier(this);
        if (controller.TryGetComponent<ShipEquipment>(out var ship))
            ship.OnCommandExecuted.AddListener(SelfRemove);
    }

    public override void OnUnequip(PassiveAbilityController controller)
    {
        if (controller.TryGetComponent<OffensiveEquipment>(out var eq))
            eq.RemoveAccuracyModifier(this);
        if (controller.TryGetComponent<ShipEquipment>(out var ship))
            ship.OnCommandExecuted.RemoveListener(SelfRemove);
        _controller = null;
    }

    private void SelfRemove() => _controller?.RemovePassive(this);
}
