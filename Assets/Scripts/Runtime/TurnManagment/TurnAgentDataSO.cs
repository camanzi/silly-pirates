using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Turn System/Agent Data" )]
public class TurnAgentDataSO : ScriptableObject
{
    [Header("Turn Agent Data Config")]
    [Tooltip("Value on which evaluate Action Value during fight")]
    [SerializeField] private int _initialAgility;
    [SerializeField] private int _maxActionPointsPerTurn = 5;
    [SerializeField] private float _maxHp = 100f;
    
    // FIXME Later Attenzione! Questo oggetto dovrebbe contenere SOLO le info riguardanti i turni
    // Questo é un dato SOLO della griglia e quindi NON condiviso con i nemici!
    [Tooltip("Maximun movement on grid per turn")]
    [SerializeField] private int _maxMovementPoints;

    [Tooltip("Interaction range")]
    [SerializeField] private int _interactionRange = 1;

    [Tooltip("Number of consecutive actions this agent takes per turn cycle")]
    [SerializeField] private int _actionsPerTurn = 1;

    [SerializeField] private int _baseEvasion = 50;

    public int InitialAgility => _initialAgility;
    public int MaxActionPointsPerTurn => _maxActionPointsPerTurn;
    public int MaxMovementPoints => _maxMovementPoints;
    public int InteractionRange => _interactionRange;
    public float MaxHp => _maxHp;
    public int ActionsPerTurn => _actionsPerTurn;
    public int BaseEvasion => _baseEvasion;
}
