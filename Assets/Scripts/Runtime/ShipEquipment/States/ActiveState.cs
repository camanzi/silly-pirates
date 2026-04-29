using UnityEngine;

public class ActiveState : EquipmentState
{
    public ActiveState(EquipmentStateMachine stateMachine, IAwakable context) : base(stateMachine, context)
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
    }
}