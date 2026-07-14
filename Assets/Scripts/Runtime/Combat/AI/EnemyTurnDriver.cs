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

            await Awaitable.WaitForSecondsAsync(.5f, token);

            _currentTurnStateData.SignalTurnEnd();
        }
    }

    private async Awaitable RaiseCameraCueAsync()
    {
        if (_cameraDirectorState == null || _cameraCueChannel == null) return;
        if (!_agent.GetVariable("SelectedAbility", out BlackboardVariable<AbilityBase> abilityVar)) return;
        AbilityBase ability = abilityVar.Value;
        if (ability == null) return;

        _agent.GetVariable("SelectedTarget", out BlackboardVariable<MonoBehaviour> targetVar);
        MonoBehaviour targetMono = targetVar?.Value;
        var targets = targetMono is ITargettable target
            ? new List<ITargettable> { target }
            : null;
        Vector3? targetPoint = targetMono != null ? targetMono.transform.position : null;

        _cameraDirectorState.BeginFocus();
        _cameraCueChannel.RaiseEvent(new AbilityExecutionCue(ability, _hostile, targets, affectedCells: null, targetPoint));
        await _cameraDirectorState.WaitUntilFocused();
    }
}
