using UnityEngine;

public abstract class EquipmentStatsSO : ScriptableObject
{
    [SerializeField] private int _baseEvasion = 0;

    public abstract EquipmentType EquipmentType { get; }
    public int BaseEvasion => _baseEvasion;
}
