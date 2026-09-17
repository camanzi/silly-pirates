using UnityEngine;

[CreateAssetMenu(fileName = "Accuracy Overcap Passive", menuName = "Abilities/Equipment/Accuracy Overcap Passive")]
public class AccuracyOvercapPassiveSO : PassiveAbilitySO, IAccuracyModifier, IOvercapPassive, IStackablePassive, IStackCountProvider
{
    private int _accuracyBonus;
    private int _applications;

    // Purely cosmetic counter: it feeds the "x2"/"x3" on the popup and the badge.
    // The bonus does not add up, it is overwritten on every reapplication.
    public int CurrentStacks => _applications;
    public int MaxStacks => int.MaxValue;

    public void Initialize(int bonus) => _accuracyBonus = bonus;

    int IAccuracyModifier.GetAccuracyBonus() => _accuracyBonus;

    public override void OnEquip(PassiveAbilityController controller)
    {
        _applications = 1;
        if (controller.TryGetComponent<OffensiveEquipment>(out var eq))
            eq.AddAccuracyModifier(this);
    }

    public override void OnUnequip(PassiveAbilityController controller)
    {
        if (controller.TryGetComponent<OffensiveEquipment>(out var eq))
            eq.RemoveAccuracyModifier(this);
    }

    // Does not stack: the bonus is recomputed from the overcap points every time, so
    // reapplying overwrites the value. The existing instance stays registered as a
    // modifier, with no OnUnequip/OnEquip cycle.
    void IStackablePassive.OnReapplied(PassiveAbilityController controller, PassiveAbilitySO incoming)
    {
        if (incoming is not AccuracyOvercapPassiveSO overcap) return;

        _accuracyBonus = overcap._accuracyBonus;
        _applications++;
    }
}
