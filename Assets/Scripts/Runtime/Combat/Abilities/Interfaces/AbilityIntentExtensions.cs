/// <summary>
/// Unico punto di lettura dell'intent di un'abilita'. Il cast a <see cref="IAbilityIntent"/> e' necessario:
/// <c>Intent</c> e' un default interface member e non esiste sul tipo concreto.
/// </summary>
public static class AbilityIntentExtensions
{
    public static AbilityIntent GetIntent(this AbilityBase ability)
        => (ability as IAbilityIntent)?.Intent ?? AbilityIntent.Utility;
}
