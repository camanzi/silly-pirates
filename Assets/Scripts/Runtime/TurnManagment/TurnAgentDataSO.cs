using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Turn System/Agent Data" )]
public class TurnAgentDataSO : ScriptableObject
{
    [Header("Turn Agent Data Config")]
    [Tooltip("Value on which evaluate Action Value during fight")]
    [SerializeField] private int _initialAgility;
    [SerializeField] private int _maxActionPointsPerTurn = 5;
    [SerializeField] private float _maxHp = 100f;
    
    // FIXME Later. Careful! This object should hold ONLY turn-related information.
    // This is grid-only data and therefore NOT shared with the enemies!
    [Tooltip("Maximun movement on grid per turn")]
    [SerializeField] private int _maxMovementPoints;

    [Tooltip("Interaction range")]
    [SerializeField] private int _interactionRange = 1;

    [Tooltip("Number of consecutive actions this agent takes per turn cycle")]
    [SerializeField] private int _actionsPerTurn = 1;

    [SerializeField] private int _baseEvasion = 50;

    [Tooltip("When on, the first Action Value assigned in the turn queue on join (spawn) varies " +
             "pseudo-randomly around the base value, to give the turn order some variety. " +
             "From the second turn onwards the AV always returns to the base value.")]
    [SerializeField] private bool _randomizeInitialAV;

    public int InitialAgility => _initialAgility;
    public int MaxActionPointsPerTurn => _maxActionPointsPerTurn;
    public int MaxMovementPoints => _maxMovementPoints;
    public int InteractionRange => _interactionRange;
    public float MaxHp => _maxHp;
    public int ActionsPerTurn => _actionsPerTurn;
    public int BaseEvasion => _baseEvasion;
    public bool RandomizeInitialAV => _randomizeInitialAV;
}
