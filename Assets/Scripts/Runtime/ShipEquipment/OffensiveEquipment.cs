using System.Collections.Generic;

public class OffensiveEquipment : ShipEquipment, ICritDMGOwner, IDMGTypeOwner
{
    private readonly List<ICritDMGModifier> _critDMGModifiers = new();

    public int EffectiveCritDMG
    {
        get
        {
            int total = (StatsConfig as IOffensiveEquipmentStats)?.CritDMG ?? 0;
            for (int i = 0; i < _critDMGModifiers.Count; i++)
                total += _critDMGModifiers[i].GetCritDMGBonus();
            return total;
        }
    }

    public void AddCritDMGModifier(ICritDMGModifier modifier) => _critDMGModifiers.Add(modifier);
    public void RemoveCritDMGModifier(ICritDMGModifier modifier) => _critDMGModifiers.Remove(modifier);

    private readonly List<IDMGTypeModifier> _dmgTypeModifiers = new();

    public DamageType EffectiveDMGType =>
        _dmgTypeModifiers.Count > 0
            ? _dmgTypeModifiers[^1].GetDMGTypeOverride()
            : (StatsConfig as IOffensiveEquipmentStats)?.DMGType ?? DamageType.None;

    public void AddDMGTypeModifier(IDMGTypeModifier m) => _dmgTypeModifiers.Add(m);
    public void RemoveDMGTypeModifier(IDMGTypeModifier m) => _dmgTypeModifiers.Remove(m);
}
