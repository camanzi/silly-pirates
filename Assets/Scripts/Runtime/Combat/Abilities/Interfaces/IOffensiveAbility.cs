/// <summary>
/// Abilita' ostile verso il proprio bersaglio: danno, debuff, controllo. E' un'interfaccia e non una classe
/// base perche' la gerarchia e' gia' impegnata — le abilita' offensive nemiche devono estendere
/// EnemyAbilityBase e non potrebbero ereditare anche da OffensiveAbilityBase.
/// </summary>
public interface IOffensiveAbility : IAbilityIntent
{
    AbilityIntent IAbilityIntent.Intent => AbilityIntent.Offensive;

    /// <summary>
    /// Elemento con cui questa abilita' colpira', dato il caster. Non e' una property secca perche' per le
    /// abilita' da equipaggiamento l'elemento dipende dal cannone montato, non dall'asset dell'abilita'
    /// (vedi <see cref="DamageTypeResolver"/>).
    ///
    /// <see cref="DamageType.None"/> significa "ostile ma senza elemento": una rete, una maledizione, o
    /// un'abilita' composita il cui elemento vive negli step e non e' risolvibile da qui.
    /// </summary>
    DamageType ResolveDamageElement(IInteractableElement caster);
}
