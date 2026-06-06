using System.Threading;
using Unity.Behavior;
using UnityEngine;

public class EnemyTurnDriver : MonoBehaviour
{
    [SerializeField] private EnemyAIDataSO _aiData;
    [SerializeField] private TurnStateSO _currentTurnStateData;
    [SerializeField] private CommandQueueSO _commandQueue;

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

            await _commandQueue.ProcessQueueAsync();
        }
        finally
        {
            await Awaitable.WaitForSecondsAsync(.5f, token);

            _currentTurnStateData.SignalTurnEnd();
        }
    }
}
