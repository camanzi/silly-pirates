/// <summary>
/// Common base of <see cref="IOffensiveAbility"/> and <see cref="IDefensiveAbility"/>. It is not meant to be
/// implemented directly: the two derived interfaces supply <see cref="Intent"/> as a default interface
/// member, so an AbilityBase subclass declares its own category without writing a single line of body.
///
/// To read it, use <see cref="AbilityIntentExtensions.GetIntent"/>: a default interface member is not
/// visible on the concrete type and has to be reached through the interface.
/// </summary>
public interface IAbilityIntent
{
    AbilityIntent Intent { get; }
}
