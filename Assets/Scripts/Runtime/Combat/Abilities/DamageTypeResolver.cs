/// <summary>
/// The single rule that resolves the element of a hit: the caster's equipment wins over the base type
/// declared by the ability. Extracted here because it is used both by the command that deals the damage and
/// by <see cref="IOffensiveAbility.ResolveDamageElement"/>, which has to announce up front the same element
/// that will actually land: were the two copies to diverge, the elemental shield would parry the wrong
/// hit.
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
