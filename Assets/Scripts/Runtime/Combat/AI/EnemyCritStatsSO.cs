using UnityEngine;

[CreateAssetMenu(fileName = "Enemy Crit Stats", menuName = "Combat/AI/Enemy Crit Stats")]
public class EnemyCritStatsSO : ScriptableObject
{
    [SerializeField] [Range(0, 100)] private int _critRate;
    [SerializeField] [Range(0, 400)] private int _critDMG = 50;

    public int CritRate => _critRate;
    public int CritDMG  => _critDMG;
}
