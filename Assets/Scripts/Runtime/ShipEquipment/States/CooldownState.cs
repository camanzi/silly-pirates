using UnityEngine;

public class CooldownState : EquipmentState
{
    public CooldownState(EquipmentStateMachine stateMachine, IAwakable context) : base(stateMachine, context)
    {
        
    }

    public override void OnStateEnter()
    {
        base.OnStateEnter();
    }
    public override void OnStateUpdate()
    {
        base.OnStateUpdate();
    }
    public override void OnStateExit()
    {
        base.OnStateExit();
    }
    public override void OnTurnChanged()
    {
        base.OnTurnChanged();
        int newCooldown = _context.Cooldown - 1;
        if (newCooldown <= 0) _stateMachine.TransitionTo(new AwakableState(_stateMachine, _context));
        _context.Cooldown = newCooldown;
    }
}