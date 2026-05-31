using UnityEngine;

public abstract class PassiveAbilitySO : ScriptableObject
{
    [SerializeField] private string _displayName;
    public virtual string DisplayName => _displayName;

    public abstract void OnEquip(PassiveAbilityController controller);
    public abstract void OnUnequip(PassiveAbilityController controller);
}
