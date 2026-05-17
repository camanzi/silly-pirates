using UnityEngine;

[CreateAssetMenu(fileName = "Offensive Equipment Stats", menuName = "Equipment/Offensive Stats")]
public class OffensiveEquipmentStatsSO : EquipmentStatsSO, IOffensiveEquipmentStats
{
    [SerializeField] private int _baseDMG;
    [SerializeField] [Range(0, 100)] private int _critRate;
    [SerializeField] [Range(0, 400)] private int _critDMG = 50;
    [SerializeField] private DamageType _dmgType;

    public override EquipmentType EquipmentType => EquipmentType.Offensive;
    public int BaseDMG => _baseDMG;
    public int CritRate => _critRate;
    public int CritDMG => _critDMG;
    public DamageType DMGType => _dmgType;
}
