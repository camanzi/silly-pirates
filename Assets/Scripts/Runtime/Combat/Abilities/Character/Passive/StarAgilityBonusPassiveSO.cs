public class StarAgilityBonusPassiveSO : PassiveAbilitySO, IAgilityModifier
{
    public int FlatBonus = 0;
    public float PercentageBonus = 0f;

    public int GetFlatAgilityBonus() => FlatBonus;
    public float GetPercentageAgilityBonus() => PercentageBonus;

    public override void OnEquip(PassiveAbilityController controller) { }
    public override void OnUnequip(PassiveAbilityController controller) { }
}
