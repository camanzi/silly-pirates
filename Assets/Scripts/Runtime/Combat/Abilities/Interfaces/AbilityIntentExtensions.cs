/// <summary>
/// The single place an ability's intent is read from. The cast to <see cref="IAbilityIntent"/> is required:
/// <c>Intent</c> is a default interface member and does not exist on the concrete type.
/// </summary>
public static class AbilityIntentExtensions
{
    public static AbilityIntent GetIntent(this AbilityBase ability)
        => (ability as IAbilityIntent)?.Intent ?? AbilityIntent.Utility;
}
