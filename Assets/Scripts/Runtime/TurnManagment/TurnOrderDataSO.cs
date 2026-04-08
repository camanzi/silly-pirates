using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "TurnOrderData", menuName = "Combat/Turn System/Turn Order")]
public class TurnOrderDataSO : ScriptableObject
{
    public List<EntityTurnState> TurnQueue = new();
    
    [Space]
    public UnityEvent OnQueueUpdated;

    public void CompleteActiveTurn()
    {
        if (TurnQueue.Count < 2) return;

        EntityTurnState finishedEntity = TurnQueue[0];
        TurnQueue.RemoveAt(0);

        float timePassed = TurnQueue[0].CurrentAV;

        foreach (EntityTurnState state in TurnQueue)
        {
            state.CurrentAV -= timePassed;
            state.CurrentAV = Mathf.Max(0, state.CurrentAV);
        }

        finishedEntity.CurrentAV = CalculateBaseAV(finishedEntity.Agent);
        TurnQueue.Add(finishedEntity);

        SortQueue();
    }

    public void AddEntity(ITurnAgent agent)
    {
        if (TurnQueue.Exists(e => e.Agent == agent)) return;

        float initialAV = CalculateBaseAV(agent);
        
        EntityTurnState newState = new EntityTurnState(agent, initialAV);
        TurnQueue.Add(newState);

        if (agent is MonoBehaviour mono)
            Debug.Log($"Ho aggiunto un nuovo Agent {mono.name} con AV: {initialAV}");

        SortQueue();
    }

    public void RemoveEntity(ITurnAgent agent)
    {
        int index = TurnQueue.FindIndex(e => e.Agent == agent);

        if (index != -1)
        {
            TurnQueue.RemoveAt(index);
            
            OnQueueUpdated?.Invoke();
        }
    }

    public void Clear()
    {
        TurnQueue.Clear();
        OnQueueUpdated?.Invoke();
    }

    private float CalculateBaseAV(ITurnAgent a)
    {
        float speed = Mathf.Max(1, a.AgentData.MaxMoveSpeed);
        return 10000f / speed;
    } 
    private void SortQueue()
    {
        TurnQueue.Sort((a, b) => {
            int result = a.CurrentAV.CompareTo(b.CurrentAV);
            
            if (result == 0)
                return b.Agent.AgentData.InitialAgility.CompareTo(a.Agent.AgentData.InitialAgility);
            
            return result;
        });
        
        OnQueueUpdated?.Invoke();
    }
}