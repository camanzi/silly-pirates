using UnityEngine;

// FIXME Later, sarebbe da far diventare piú universale, tipo EquipmentController
[RequireComponent(typeof(ShootingEquipment))]
public class EquipmentStateMachine : MonoBehaviour
{
    [SerializeField] private VoidEventChannel _onEquipmentAwakenedEventChannel;
    protected IAwakable shootingEquipment;
    public bool IsActive => _currentState is ActiveState;
    public bool IsOnCooldown => _currentState is CooldownState;
    public VoidEventChannel OnEquipmentAwakenedEventChannel => _onEquipmentAwakenedEventChannel;
    private EquipmentState _currentState;

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
        _currentState?.OnStateExit();
        _currentState = newState;
        _currentState.OnStateEnter();
    }

    public void OnTurnChange() => _currentState.OnTurnChanged();
}
