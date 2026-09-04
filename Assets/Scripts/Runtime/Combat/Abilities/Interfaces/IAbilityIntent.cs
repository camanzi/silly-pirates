/// <summary>
/// Base comune di <see cref="IOffensiveAbility"/> e <see cref="IDefensiveAbility"/>. Non va implementata
/// direttamente: le due interfacce derivate forniscono <see cref="Intent"/> come default interface member,
/// quindi una sottoclasse di AbilityBase dichiara la propria categoria senza scrivere una riga di corpo.
///
/// Per leggerla usare <see cref="AbilityIntentExtensions.GetIntent"/>: un default interface member non e'
/// visibile sul tipo concreto e va raggiunto attraverso l'interfaccia.
/// </summary>
public interface IAbilityIntent
{
    AbilityIntent Intent { get; }
}
