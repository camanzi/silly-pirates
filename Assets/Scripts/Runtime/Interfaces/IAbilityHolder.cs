public interface IAbilityHolder
{
    AbilityController ActiveAbilityController { get; }
    PassiveAbilityController PassiveAbilityController { get; }
}
