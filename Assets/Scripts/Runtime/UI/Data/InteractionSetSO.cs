using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewInteractionSet", menuName = "UI/Interactions/Set")]
public class InteractionSetSO : ScriptableObject
{
    [Header("Main Action")]
    [SerializeField] private InteractionActionSO _mainAction;
    [SerializeField] private Sprite _cooldownIcon;

    [Header("Secondary Actions")]
    [SerializeField] private List<InteractionActionSO> _availableActions;

    public InteractionActionSO MainAction => _mainAction;
    public Sprite CooldownIcon => _cooldownIcon;
    public List<InteractionActionSO> AvailableActions => _availableActions;
}