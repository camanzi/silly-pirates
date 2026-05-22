using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyAIData", menuName = "Combat/AI/Enemy AI Data")]
public class EnemyAIDataSO : ScriptableObject
{
    [SerializeField] private List<EnemyAbilityBase> _abilities;
    [SerializeField] private TurnOrderDataSO _turnOrderData;
    [SerializeField] private GridStateDataSO _gridStateData;

    public IReadOnlyList<EnemyAbilityBase> Abilities => _abilities;
    public TurnOrderDataSO TurnOrderData => _turnOrderData;
    public GridStateDataSO GridStateData => _gridStateData;
}
