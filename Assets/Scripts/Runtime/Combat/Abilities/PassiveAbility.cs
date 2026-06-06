using UnityEngine;

public abstract class PassiveAbilitySO : ScriptableObject
{
    [SerializeField] private string _displayName;
    [SerializeField] private PassiveRemovalTiming _removalTiming = PassiveRemovalTiming.AnyTurn;

    public virtual string DisplayName => _displayName;
    public PassiveRemovalTiming RemovalTiming => _removalTiming;

    public abstract void OnEquip(PassiveAbilityController controller);
    public abstract void OnUnequip(PassiveAbilityController controller);
}
