using System.Collections.Generic;

public class OffensiveEquipment : ShipEquipment, IAccuracyOwner, IDMGTypeOwner
{
    private readonly List<IAccuracyModifier> _accuracyModifiers = new();

    public int EffectiveAccuracy
    {
        get
        {
            int total = (StatsConfig as IOffensiveEquipmentStats)?.BaseAccuracy ?? 0;
            for (int i = 0; i < _accuracyModifiers.Count; i++)
                total += _accuracyModifiers[i].GetAccuracyBonus();
            return total;
        }
    }

    public void AddAccuracyModifier(IAccuracyModifier modifier) => _accuracyModifiers.Add(modifier);
    public void RemoveAccuracyModifier(IAccuracyModifier modifier) => _accuracyModifiers.Remove(modifier);

    private readonly List<IDMGTypeModifier> _dmgTypeModifiers = new();

    public DamageType EffectiveDMGType =>
        _dmgTypeModifiers.Count > 0
            ? _dmgTypeModifiers[^1].GetDMGTypeOverride()
            : DamageType.None;

    public void AddDMGTypeModifier(IDMGTypeModifier m) => _dmgTypeModifiers.Add(m);
    public void RemoveDMGTypeModifier(IDMGTypeModifier m) => _dmgTypeModifiers.Remove(m);
}
