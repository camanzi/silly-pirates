using UnityEngine;

public abstract class EquipmentState
{
    protected IAwakable _context;
    protected EquipmentStateMachine _stateMachine;
    public EquipmentState(EquipmentStateMachine stateMachine, IAwakable context)
    {
        _context = context;
        _stateMachine = stateMachine;
    }

    public virtual void OnStateEnter() { }
    public virtual void OnStateUpdate() { }
    public virtual void OnStateExit() { }
    public virtual void OnTurnChanged() { }
}