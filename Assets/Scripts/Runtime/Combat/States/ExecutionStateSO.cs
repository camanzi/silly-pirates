using UnityEngine;

[CreateAssetMenu(menuName = "Combat/States/Execution")]
public class ExecutionStateSO : CombatStateSO
{
    [SerializeField] private CombatStateSO _idleStateTemplate;

    public async override void OnEnter()
    {
        base.OnEnter();

        CameraDirectorStateSO cameraState = manager.CameraDirectorState;
        try
        {
            AbilityExecutionCue? cue = manager.CombatCtx.PendingCue;
            if (cue.HasValue && cameraState != null && manager.CameraCueChannel != null)
            {
                cameraState.BeginFocus();
                manager.CameraCueChannel.RaiseEvent(cue.Value);
                await cameraState.WaitUntilFocused();
            }

            await manager.CommandQueue.ProcessQueueAsync();
        }
        finally
        {
            cameraState?.EndFocus();
        }

        manager.AabilityRenderer.ClearPreview();
        manager.ClearCtxs(_idleStateTemplate);
        Debug.Log($"Sono entrato da Execution state");
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