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

            // Fire-and-forget: a differenza del camera cue, l'audio non blocca mai il turn loop.
            RaiseCastSfx(cue);

            // Prima dell'attesa sulla camera, non dopo: i bersagli devono potersi preparare mentre
            // l'inquadratura si compone, non nell'istante dell'impatto.
            RaiseThreatBegin(cue);

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
            manager.ThreatChannel?.RaiseEvent(AbilityThreatCue.End);
        }

        manager.AabilityRenderer.ClearPreview();
        manager.ClearCtxs(_idleStateTemplate);
        Debug.Log($"Sono entrato da Execution state");
    }
    public override void OnExit()
    {
        Debug.Log($"Sono uscito dal Execution state");
    }

    private void RaiseThreatBegin(AbilityExecutionCue? cue)
    {
        if (!cue.HasValue || manager.ThreatChannel == null) return;

        AbilityBase ability = cue.Value.Ability;
        if (ability == null) return;

        manager.ThreatChannel.RaiseEvent(
            AbilityThreatCue.Begin(ability, cue.Value.Caster, cue.Value.Targets));
    }

    private void RaiseCastSfx(AbilityExecutionCue? cue)
    {
        if (!cue.HasValue) return;

        SfxCueEventChannel channel = manager.SfxChannel;
        if (channel == null) return;

        AbilityBase ability = cue.Value.Ability;
        if (ability == null || ability.CastSfx == null) return;

        Vector3 position = cue.Value.Caster?.Transform != null
            ? cue.Value.Caster.Transform.position
            : cue.Value.TargetPoint ?? Vector3.zero;

        channel.RaiseEvent(SfxCue.At(ability.CastSfx, position));
    }

    public override void OnUpdate() { }

    public override void HandleElementClick(IInteractableElement element) { }

    public override void HandlePointerMove(TargetingData data) { }

    public override void HandleGlobalClick(TargetingData data) { }

    public override void HandleRightClick() { }

    public override void HandleSelectAbility(IInteractableElement element) { }
}