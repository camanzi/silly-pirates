using System;
using UnityEngine;
using UnityEngine.UIElements;

[CreateAssetMenu(fileName = "Equipment Movement Boost", menuName = "Abilities/Character/Passives/Equipment Movement Boost")]
public class EquipmentMovementBoostPassiveSO : PassiveAbilitySO, IMovementModifier, IMovementBoostUIReporter, IPassiveUIProvider
{
    [Header("Event Channels")]
    [SerializeField] private VoidEventChannel _equipmentAwakenedChannel; 
    [SerializeField] private TurnAgentEventChannel _turnChangedChannel;

    [Header("UI Configs")]
    [SerializeField] private VisualTreeAsset _uiTemplate;

    private int _stacks = 0;
    private int _inactiveTurns = 0;

    public int CurrentStacks => _stacks;
    public int MaxStacks => 3;
    public int TurnsUntilDecay => Mathf.Max(0, 2 - _inactiveTurns);

    public event Action OnStateUpdated;

    public VisualTreeAsset GetTemplate() => _uiTemplate;

    public VisualElement CreateUIElement(VisualTreeAsset template) {
        return new MovementBoostIndicator(this, template);
    }

    public int GetMovementBonus() => _stacks;

    public override void OnEquip(PassiveAbilityController controller) 
    {
        _stacks = 0;
        _inactiveTurns = 0;
        
        _equipmentAwakenedChannel.OnEventRaised += OnEquipmentAwakened;
        _turnChangedChannel.OnEventRaised += (agent) => OnTurnChanged(agent, controller);
    }

    public override void OnUnequip(PassiveAbilityController controller) 
    {
        _equipmentAwakenedChannel.OnEventRaised -= OnEquipmentAwakened;
        _turnChangedChannel.OnEventRaised -= (agent) => OnTurnChanged(agent, controller);
    }

    private void OnEquipmentAwakened() 
    {
        _inactiveTurns = 0; 
        if (_stacks < 3) _stacks++;
        OnStateUpdated?.Invoke();
    }

    private void OnTurnChanged(ITurnAgent agent, PassiveAbilityController controller) 
    {
        if (agent.CompareTag("Player") && agent is GridCharacter) 
        {
            _inactiveTurns++;
            if (_inactiveTurns >= 2 && _stacks > 0) _stacks--;
            OnStateUpdated?.Invoke();
        }
    }
}