using UnityEngine;

public abstract class PassiveAbilitySO : ScriptableObject 
{
    public abstract void OnEquip(PassiveAbilityController controller);
    public abstract void OnUnequip(PassiveAbilityController controller);
}
