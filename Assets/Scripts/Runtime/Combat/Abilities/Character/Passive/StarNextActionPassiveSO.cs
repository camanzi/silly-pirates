using UnityEngine;

public class StarNextActionPassiveSO : PassiveAbilitySO, IAVModifier
{
    public float DiscountPercentage = 20f;

    public float GetAVDiscountPercentage() => DiscountPercentage;
    public override void OnEquip(PassiveAbilityController controller) { }
    public override void OnUnequip(PassiveAbilityController controller) { }
}
