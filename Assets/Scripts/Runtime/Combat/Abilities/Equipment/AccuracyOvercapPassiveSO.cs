using UnityEngine;

[CreateAssetMenu(fileName = "Accuracy Overcap Passive", menuName = "Abilities/Equipment/Accuracy Overcap Passive")]
public class AccuracyOvercapPassiveSO : PassiveAbilitySO, IAccuracyModifier, IOvercapPassive, IStackablePassive, IStackCountProvider
{
    private int _accuracyBonus;
    private int _applications;
    private PassiveAbilityController _controller;

    // Contatore puramente cosmetico: alimenta il "x2"/"x3" del popup e del badge.
    // Il bonus non si somma, viene sovrascritto a ogni riapplicazione.
    public int CurrentStacks => _applications;
    public int MaxStacks => int.MaxValue;

    public void Initialize(int bonus) => _accuracyBonus = bonus;

    int IAccuracyModifier.GetAccuracyBonus() => _accuracyBonus;

    public override void OnEquip(PassiveAbilityController controller)
    {
        _controller = controller;
        _applications = 1;
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

    // Non stacka: il bonus viene ricalcolato ogni volta dai punti di overcap,
    // quindi la riapplicazione sovrascrive il valore. L'istanza esistente resta
    // registrata come modifier, senza ciclo OnUnequip/OnEquip.
    void IStackablePassive.OnReapplied(PassiveAbilityController controller, PassiveAbilitySO incoming)
    {
        if (incoming is not AccuracyOvercapPassiveSO overcap) return;

        _accuracyBonus = overcap._accuracyBonus;
        _applications++;
    }

    private void SelfRemove() => _controller?.RemovePassive(this);
}
