public interface IStackablePassive
{
    // incoming is the instance the caller has just created and discards right afterwards:
    // it is there for the passives that overwrite their own value instead of stacking.
    void OnReapplied(PassiveAbilityController controller, PassiveAbilitySO incoming);
}
