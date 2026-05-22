using System.Threading;
using Unity.Behavior;
using UnityEngine;

public class EnemyTurnDriver : MonoBehaviour
{
    [SerializeField] private EnemyAIDataSO _aiData;
    [SerializeField] private TurnStateSO _currentTurnStateData;
    [SerializeField] private TurnController _turnController;

    private BehaviorGraphAgent _agent;
    private HostileCharacter _hostile;

    private void Awake()
    {
        _agent = GetComponent<BehaviorGraphAgent>();
        _hostile = GetComponent<HostileCharacter>();
    }

    public async Awaitable ExecuteTurnAsync(CancellationToken token)
    {
        try
        {
            _agent.SetVariableValue("Agent", (MonoBehaviour)_hostile);
            _agent.SetVariableValue("TurnControllerRef", (MonoBehaviour)_turnController);
            _agent.SetVariableValue("AIData", _aiData);

            _agent.Restart();

            while (_agent.Graph != null && _agent.Graph.IsRunning)
            {
                token.ThrowIfCancellationRequested();
                await Awaitable.NextFrameAsync(token);
            }

            await _turnController.ProcessQueueAsync();
        }
        finally
        {
            _currentTurnStateData.SignalTurnEnd();
        }
    }
}
