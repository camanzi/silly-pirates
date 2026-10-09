using System.Collections.Generic;

public class OffensiveEquipment : ShipEquipment, IDMGTypeOwner
{
    private readonly List<IDMGTypeModifier> _dmgTypeModifiers = new();

    public DamageType EffectiveDMGType =>
        _dmgTypeModifiers.Count > 0
            ? _dmgTypeModifiers[^1].GetDMGTypeOverride()
            : DamageType.None;

    public void AddDMGTypeModifier(IDMGTypeModifier m) => _dmgTypeModifiers.Add(m);
    public void RemoveDMGTypeModifier(IDMGTypeModifier m) => _dmgTypeModifiers.Remove(m);
}
