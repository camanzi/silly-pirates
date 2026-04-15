using UnityEngine;

[CreateAssetMenu(menuName = "Combat/States/Execution")]
public class ExecutionStateSO : CombatStateSO
{
    [SerializeField] private CombatStateSO _idleStateTemplate;

    public async override void OnEnter()
    {
        await manager.TurnController.ProcessQueueAsync();

        manager.TurnController.ClearPreview();
        manager.ClearCtxs(_idleStateTemplate);
    }
    public override void OnExit()
    {
        Debug.Log($"Sono uscito dal Execution state");
    }

    public override void OnUpdate() { }

    public override void HandleElementClick(IInteractableElement element) { }

    public override void HandlePointerMove(TargetingData data) { }

    public override void HandleGlobalClick(TargetingData data) { }

    public override void HandleRightClick() { }

    public override void HandleSelectAbility(IInteractableElement element) { }
}