using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewInteractionSet", menuName = "UI/Interactions/Set")]
public class InteractionSetSO : ScriptableObject
{
    [SerializeField] private List<InteractionActionSO> _availableActions;

    public List<InteractionActionSO> AvailableActions => _availableActions;
}