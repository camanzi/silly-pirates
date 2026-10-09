using System;
using UnityEngine;

[RequireComponent(typeof(ShipEquipment))]
public class EquipmentStateMachine : MonoBehaviour
{
    [SerializeField] private VoidEventChannel _onEquipmentAwakenedEventChannel;
    protected IAwakable shootingEquipment;
    public bool IsActive => _currentState is ActiveState;
    public bool IsOnCooldown => _currentState is CooldownState;
    public VoidEventChannel OnEquipmentAwakenedEventChannel => _onEquipmentAwakenedEventChannel;
    private EquipmentState _currentState;

    /// <summary>
    /// Raised only when <see cref="IsActive"/> actually flips: true on awakening, false when the equipment
    /// leaves the active state (cooldown after firing, an undone awakening). Unlike the awakened channel it
    /// is per instance, so the equipment itself can react (e.g. arming the Broadside).
    /// </summary>
    public event Action<bool> OnActiveChanged;

    protected void Awake()
    {
        shootingEquipment = GetComponent<IAwakable>();
    }

    void Start()
    {
        TransitionTo(new AwakableState(this, shootingEquipment));
    }

    public void TransitionTo(EquipmentState newState)
    {
        bool wasActive = IsActive;

        _currentState?.OnStateExit();
        _currentState = newState;
        _currentState.OnStateEnter();

        bool isActive = IsActive;
        if (isActive != wasActive) OnActiveChanged?.Invoke(isActive);
    }

    public void OnTurnChange() => _currentState.OnTurnChanged();
}
