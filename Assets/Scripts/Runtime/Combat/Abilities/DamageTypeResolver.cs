/// <summary>
/// Regola unica di risoluzione dell'elemento di un colpo: l'equipaggiamento del caster vince sul tipo base
/// dichiarato dall'abilita'. Estratta qui perche' la usano sia il comando che infligge il danno sia
/// <see cref="IOffensiveAbility.ResolveDamageElement"/>, che deve annunciare in anticipo lo stesso elemento
/// che poi arrivera' davvero: se le due copie divergessero, lo scudo elementale parerebbe il colpo
/// sbagliato.
/// </summary>
public static class DamageTypeResolver
{
    public static DamageType Resolve(IInteractableElement caster, DamageType baseType)
    {
        if (caster is IDMGTypeOwner owner)
        {
            DamageType overridden = owner.EffectiveDMGType;
            if (overridden != DamageType.None) return overridden;
        }

        return baseType;
    }
}
