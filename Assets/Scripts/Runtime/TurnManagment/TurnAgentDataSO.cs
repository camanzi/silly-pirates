using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Turn System/Agent Data" )]
public class TurnAgentDataSO : ScriptableObject
{
    [Header("Turn Agent Data Config")]
    [Tooltip("Value on which evaluate Action Value during fight")]
    [SerializeField] private int _initialAgility;
    
    // FIXME Later Attenzione! Questo oggetto dovrebbe contenere SOLO le info riguardanti i turni
    // Questo é un dato SOLO della griglia e quindi NON condiviso con i nemici!
    [Tooltip("Maximun movement on grid per turn")]
    [SerializeField] private int _maxMoveSpeed;

    public int InitialAgility => _initialAgility;
    public int MaxMoveSpeed => _maxMoveSpeed;
}
