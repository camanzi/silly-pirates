using System.Collections.Generic;
using System.Threading;
using Unity.Behavior;
using UnityEngine;

public class EnemyTurnDriver : MonoBehaviour
{
    [SerializeField] private EnemyAIDataSO _aiData;
    [SerializeField] private TurnStateSO _currentTurnStateData;
    [SerializeField] private CommandQueueSO _commandQueue;
    [SerializeField] private CameraDirectorStateSO _cameraDirectorState;
    [SerializeField] private AbilityExecutionCueEventChannel _cameraCueChannel;
    [SerializeField] private SfxCueEventChannel _sfxChannel;
    [Tooltip("Annuncia ai bersagli che un'abilita' sta per colpirli, e quando l'esecuzione e' finita")]
    [SerializeField] private AbilityThreatEventChannel _threatChannel;

    private BehaviorGraphAgent _agent;
    private HostileCharacter _hostile;

    private void Awake()
    {
        _agent = GetComponent<BehaviorGraphAgent>();
        _hostile = GetComponent<HostileCharacter>();
    }

    public async Awaitable ExecuteTurnAsync(CancellationToken token)
    {
        await Awaitable.WaitForSecondsAsync(1f, token);
        try
        {
            _agent.SetVariableValue("Agent", (MonoBehaviour)_hostile);
            _agent.SetVariableValue("CommandQueueRef", _commandQueue);
            _agent.SetVariableValue("AIData", _aiData);

            _agent.Restart();

            while (_agent.Graph != null && _agent.Graph.IsRunning)
            {
                token.ThrowIfCancellationRequested();
                await Awaitable.NextFrameAsync(token);
            }

            await RaiseCameraCueAsync();
            await _commandQueue.ProcessQueueAsync();
        }
        finally
        {
            _cameraDirectorState?.EndFocus();
            _threatChannel?.RaiseEvent(AbilityThreatCue.End);

            await Awaitable.WaitForSecondsAsync(.5f, token);

            _currentTurnStateData.SignalTurnEnd();
        }
    }

    private async Awaitable RaiseCameraCueAsync()
    {
        if (!_agent.GetVariable("SelectedAbility", out BlackboardVariable<AbilityBase> abilityVar)) return;
        AbilityBase ability = abilityVar.Value;
        if (ability == null) return;

        // Fire-and-forget, indipendente dalla regia di camera: l'audio non blocca mai il turn loop.
        RaiseCastSfx(ability);

        _agent.GetVariable("SelectedTarget", out BlackboardVariable<MonoBehaviour> targetVar);
        MonoBehaviour targetMono = targetVar?.Value;
        var targets = targetMono is ITargettable target
            ? new List<ITargettable> { target }
            : null;

        // Risolto e alzato PRIMA della guardia sulla camera: un nemico senza regia di camera cablata deve
        // comunque annunciare la minaccia ai propri bersagli.
        _threatChannel?.RaiseEvent(AbilityThreatCue.Begin(ability, _hostile, targets));

        if (_cameraDirectorState == null || _cameraCueChannel == null) return;

        Vector3? targetPoint = targetMono != null ? targetMono.transform.position : null;

        IReadOnlyList<Vector3> affectedCells = null;
        if (ability is IThreatenedAreaProvider provider)
            provider.TryGetThreatenedWorldPoints(_hostile, out affectedCells);

        _cameraDirectorState.BeginFocus();
        _cameraCueChannel.RaiseEvent(new AbilityExecutionCue(ability, _hostile, targets, affectedCells, targetPoint));
        await _cameraDirectorState.WaitUntilFocused();
    }

    private void RaiseCastSfx(AbilityBase ability)
    {
        if (_sfxChannel == null || ability.CastSfx == null) return;

        _sfxChannel.RaiseEvent(SfxCue.At(ability.CastSfx, transform.position));
    }
}
