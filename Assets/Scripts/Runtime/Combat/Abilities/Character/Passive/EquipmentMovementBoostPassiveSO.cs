using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "Equipment Movement Boost", menuName = "Abilities/Character/Passives/Equipment Movement Boost")]
public class EquipmentMovementBoostPassiveSO : PassiveAbilitySO, IMovementModifier
{
    [Header("Event Channels")]
    [SerializeField] private VoidEventChannel _equipmentAwakenedChannel; 
    [SerializeField] private TurnAgentEventChannel _turnChangedChannel;

    private int _stacks = 0;
    private int _inactiveTurns = 0;

    public int GetMovementBonus()
    {
        return _stacks;
    }

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
        if (_stacks < 3) 
        {
            _stacks++;
        }
    }

    private void OnTurnChanged(ITurnAgent agent, PassiveAbilityController controller) 
    {
        if (agent.CompareTag("Player") && agent is GridCharacter) 
        {
            _inactiveTurns++;
            if (_inactiveTurns >= 2 && _stacks > 0) 
            {
                _stacks--;
            }
        }
    }
}