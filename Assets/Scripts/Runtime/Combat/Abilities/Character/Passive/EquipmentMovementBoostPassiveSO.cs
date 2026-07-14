using System;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "Equipment Movement Boost", menuName = "Abilities/Character/Passives/Equipment Movement Boost")]
public class EquipmentMovementBoostPassiveSO : PassiveAbilitySO, IMovementModifier, IPassiveStateNotifier, IStackCountProvider, IStackablePassive
{
    [Header("Event Channels")]
    [SerializeField] private VoidEventChannel _equipmentAwakenedChannel;
    [SerializeField] private TurnAgentEventChannel _turnChangedChannel;

    private int _stacks = 0;
    private bool _awakenedThisTurn = false;
    private UnityAction<ITurnAgent> _onTurnChangedHandler;

    public int CurrentStacks => _stacks;
    public int MaxStacks => 3;
    public int TurnsUntilDecay => _awakenedThisTurn ? 1 : 0;

    public event Action OnStateUpdated;

    event Action IPassiveStateNotifier.OnStateChanged
    {
        add => OnStateUpdated += value;
        remove => OnStateUpdated -= value;
    }

    public int GetMovementBonus() => _stacks;

    public override bool IsCurrentlyActive(PassiveAbilityController controller) => _stacks > 0;

    public override void OnEquip(PassiveAbilityController controller)
    {
        _stacks = 0;
        _awakenedThisTurn = false;
        
        _equipmentAwakenedChannel.OnEventRaised += OnEquipmentAwakened;
        _onTurnChangedHandler = agent => OnTurnChanged(agent, controller);
        _turnChangedChannel.OnEventRaised += _onTurnChangedHandler;
    }

    public override void OnUnequip(PassiveAbilityController controller)
    {
        _equipmentAwakenedChannel.OnEventRaised -= OnEquipmentAwakened;
        _turnChangedChannel.OnEventRaised -= _onTurnChangedHandler;
    }

    void IStackablePassive.OnReapplied(PassiveAbilityController controller) => OnEquipmentAwakened();

    private void OnEquipmentAwakened()
    {
        _awakenedThisTurn = true;
        if (_stacks < 3) _stacks++;
        OnStateUpdated?.Invoke();
    }

    private void OnTurnChanged(ITurnAgent agent, PassiveAbilityController controller)
    {
        if (agent.CompareTag("Player") && agent is GridCharacter)
        {
            if (!_awakenedThisTurn && _stacks > 0) _stacks = 0;
            _awakenedThisTurn = false;
            OnStateUpdated?.Invoke();
        }
    }
}