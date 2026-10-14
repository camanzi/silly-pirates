using UnityEngine;

[CreateAssetMenu(fileName = "Agility Booster Stats", menuName = "Equipment/Agility Booster Stats")]
public class AgilityBoosterStatsSO : EquipmentStatsSO
{
    [Tooltip("Agility added to the ship while the booster is awake. Several awake boosters add up")]
    [SerializeField] private int _flatAgilityBonus = 20;
    [Tooltip("Percentage agility bonus (20 = +20%) applied to the ship while the booster is awake, after the flat bonuses")]
    [SerializeField] private float _percentageAgilityBonus;
    [Tooltip("Crew turns the booster stays on cooldown once the ship has acted")]
    [SerializeField] [Min(1)] private int _cooldownTurns = 2;

    public override EquipmentType EquipmentType => EquipmentType.Utility;

    public int FlatAgilityBonus => _flatAgilityBonus;
    public float PercentageAgilityBonus => _percentageAgilityBonus;
    public int CooldownTurns => _cooldownTurns;
}
