using System.Collections.Generic;

public class OffensiveEquipment : ShipEquipment, IHitRateOwner, IDMGTypeOwner
{
    private readonly List<IHitRateModifier> _hitRateModifiers = new();

    public int EffectiveHitBonus
    {
        get
        {
            int total = 0;
            for (int i = 0; i < _hitRateModifiers.Count; i++)
                total += _hitRateModifiers[i].GetHitRateBonus();
            return total;
        }
    }

    public void AddHitRateModifier(IHitRateModifier modifier) => _hitRateModifiers.Add(modifier);
    public void RemoveHitRateModifier(IHitRateModifier modifier) => _hitRateModifiers.Remove(modifier);

    private readonly List<IDMGTypeModifier> _dmgTypeModifiers = new();

    public DamageType EffectiveDMGType =>
        _dmgTypeModifiers.Count > 0
            ? _dmgTypeModifiers[^1].GetDMGTypeOverride()
            : DamageType.None;

    public void AddDMGTypeModifier(IDMGTypeModifier m) => _dmgTypeModifiers.Add(m);
    public void RemoveDMGTypeModifier(IDMGTypeModifier m) => _dmgTypeModifiers.Remove(m);
}
