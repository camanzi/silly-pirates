using UnityEngine;

public abstract class EquipmentStatsSO : ScriptableObject
{
    [SerializeField] private int _baseEvasion = 0;

    public abstract EquipmentType EquipmentType { get; }
    public int BaseEvasion => _baseEvasion;

    public virtual PassiveAbilitySO OvercapPassiveTemplate => null;
    public virtual int GetOvercapBonus(int extraPoints) => (int)MathUtils.CalculateOvercapBonus(extraPoints);
}
