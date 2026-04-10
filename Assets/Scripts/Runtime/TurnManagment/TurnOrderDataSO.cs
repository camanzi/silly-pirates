using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

[CreateAssetMenu(fileName = "TurnOrderData", menuName = "Combat/Turn System/Turn Order")]
public class TurnOrderDataSO : ScriptableObject
{
    [SerializeField] private VoidEventChannel _onQueueUpdated;
    
    public VoidEventChannel OnQueueUpdated => _onQueueUpdated; 

    private List<EntityTurnState> _turnQueue = new();
    public ReadOnlyCollection<EntityTurnState> TurnQueue => _turnQueue.AsReadOnly();

    public void CompleteActiveTurn()
    {
        if (_turnQueue.Count < 2) return;

        EntityTurnState finishedEntity = _turnQueue[0];
        _turnQueue.RemoveAt(0);

        float timePassed = _turnQueue[0].CurrentAV;

        foreach (EntityTurnState state in _turnQueue)
        {
            state.CurrentAV -= timePassed;
            // Minimo valore che puó assumere l'AV
            state.CurrentAV = Mathf.Max(1, state.CurrentAV);
        }

        finishedEntity.CurrentAV = CalculateBaseAV(finishedEntity.Agent);
        _turnQueue.Add(finishedEntity);

        SortQueue();
    }

    public void AddEntity(ITurnAgent agent)
    {
        if (_turnQueue.Exists(e => e.Agent == agent)) return;

        float initialAV = CalculateBaseAV(agent);
        
        EntityTurnState newState = new EntityTurnState(agent, initialAV);
        _turnQueue.Add(newState);

        if (agent is MonoBehaviour mono)
            Debug.Log($"Ho aggiunto un nuovo Agent {mono.name} con AV: {initialAV}");

        SortQueue();
    }

    public void RemoveEntity(ITurnAgent agent)
    {
        int index = _turnQueue.FindIndex(e => e.Agent == agent);

        if (index != -1)
        {
            _turnQueue.RemoveAt(index);
            
            _onQueueUpdated?.RaiseEvent();
        }
    }

    public void Clear()
    {
        _turnQueue.Clear();
        _onQueueUpdated?.RaiseEvent();
    }

    private float CalculateBaseAV(ITurnAgent a)
    {
        float speed = Mathf.Max(1, a.AgentData.InitialAgility);
        return 10000f / speed;
    } 
    private void SortQueue()
    {
        _turnQueue.Sort((a, b) => {
            int result = a.CurrentAV.CompareTo(b.CurrentAV);
            
            if (result == 0)
                return b.Agent.AgentData.InitialAgility.CompareTo(a.Agent.AgentData.InitialAgility);
            
            return result;
        });
        
        _onQueueUpdated?.RaiseEvent();
    }
}