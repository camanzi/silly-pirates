using UnityEngine;

/// <summary>
/// Per-equipment stats. Currently empty beyond the type: kept on purpose as the placeholder the upcoming
/// stats system will fill.
/// </summary>
public abstract class EquipmentStatsSO : ScriptableObject
{
    public abstract EquipmentType EquipmentType { get; }
}
