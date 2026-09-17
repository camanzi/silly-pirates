/// <summary>
/// An ability hostile to its target: damage, debuffs, control. It is an interface and not a base class
/// because the hierarchy is already taken — offensive enemy abilities have to extend EnemyAbilityBase and
/// could not inherit from an OffensiveAbilityBase as well.
/// </summary>
public interface IOffensiveAbility : IAbilityIntent
{
    AbilityIntent IAbilityIntent.Intent => AbilityIntent.Offensive;

    /// <summary>
    /// The element this ability will strike with, given the caster. It is not a plain property because for
    /// equipment abilities the element depends on the mounted cannon, not on the ability asset
    /// (see <see cref="DamageTypeResolver"/>).
    ///
    /// <see cref="DamageType.None"/> means "hostile but elementless": a net, a curse, or a composite
    /// ability whose element lives in its steps and cannot be resolved from here.
    /// </summary>
    DamageType ResolveDamageElement(IInteractableElement caster);
}
