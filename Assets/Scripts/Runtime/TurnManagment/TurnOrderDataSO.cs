using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

[CreateAssetMenu(fileName = "TurnOrderData", menuName = "Combat/Turn System/Turn Order")]
public class TurnOrderDataSO : ScriptableObject, ICombatSessionResettable
{
    [SerializeField] private VoidEventChannel _onQueueUpdated;

    public VoidEventChannel OnQueueUpdated => _onQueueUpdated;

    private List<EntityTurnState> _turnQueue = new();
    public ReadOnlyCollection<EntityTurnState> TurnQueue => _turnQueue.AsReadOnly();

    private readonly List<IAVModifier> _avModifierBuffer = new();

    /// <summary>
    /// Maximum variance (as a fraction of the base AV) applied to the first AV of an agent whose
    /// TurnAgentDataSO.RandomizeInitialAV is on. Change it here to tune how much variety the turn order
    /// has at the start of a combat.
    /// </summary>
    public const float InitialAVVarianceRatio = 0.2f; // ±1/5 of the base AV

    public void StartActiveTurn()
    {
        if (_turnQueue.Count == 0) return;

        float timePassed = _turnQueue[0].CurrentAV;
        _turnQueue[0].CurrentAV = 0f;

        for (int i = 1; i < _turnQueue.Count; i++)
        {
            _turnQueue[i].CurrentAV -= timePassed;
            _turnQueue[i].CurrentAV = Mathf.Max(1f, _turnQueue[i].CurrentAV);
        }

        _onQueueUpdated?.RaiseEvent();
    }

    public void CompleteActiveTurn()
    {
        if (_turnQueue.Count < 2) return;

        EntityTurnState finishedEntity = _turnQueue[0];
        _turnQueue.RemoveAt(0);

        float av = CalculateBaseAV(finishedEntity.Agent);
        av = ApplyAndConsumeAVDiscount(finishedEntity.Agent, av);
        finishedEntity.CurrentAV = av;

        _turnQueue.Add(finishedEntity);
        SortQueue();

        _onQueueUpdated?.RaiseEvent();
    }

    private float ApplyAndConsumeAVDiscount(ITurnAgent agent, float baseAV)
    {
        if (agent is not IAbilityHolder holder) return baseAV;
        holder.PassiveAbilityController.GetModifiers(_avModifierBuffer);
        if (_avModifierBuffer.Count == 0) return baseAV;

        float totalDiscount = 0f;
        foreach (var mod in _avModifierBuffer)
            totalDiscount += mod.GetAVDiscountPercentage();

        foreach (var mod in _avModifierBuffer)
            if (mod is PassiveAbilitySO passive)
                holder.PassiveAbilityController.RemovePassive(passive);

        return baseAV * (1f - Mathf.Clamp01(totalDiscount / 100f));
    }

    public void AddEntity(ITurnAgent agent)
    {
        if (_turnQueue.Exists(e => e.Agent == agent)) return;

        float initialAV = CalculateBaseAV(agent);

        bool randomized = agent.AgentData != null && agent.AgentData.RandomizeInitialAV;
        if (randomized)
        {
            float variance = initialAV * InitialAVVarianceRatio;
            initialAV = Mathf.Max(1f, initialAV + Random.Range(-variance, variance));
        }

        EntityTurnState newState = new EntityTurnState(agent, initialAV);
        _turnQueue.Add(newState);

        if (agent is MonoBehaviour mono)
            Debug.Log($"Added a new Agent {mono.name} with AV: {initialAV}{(randomized ? " (randomized)" : "")}");

        SortQueue();
        _onQueueUpdated?.RaiseEvent();
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

    public void ResetForNewCombat() => Clear();

    public void UpdateAgentAV(ITurnAgent agent)
    {
        var state = _turnQueue.Find(s => s.Agent == agent);
        if (state == null) return;
        state.CurrentAV = CalculateBaseAV(agent);
        SortQueue();
        _onQueueUpdated?.RaiseEvent();
    }

    public void AdjustAgentAV(ITurnAgent agent, float avDelta)
    {
        var state = _turnQueue.Find(s => s.Agent == agent);
        if (state == null || state.CurrentAV == 0f) return;
        state.CurrentAV = Mathf.Max(1f, state.CurrentAV + avDelta);
        SortQueue();
        _onQueueUpdated?.RaiseEvent();
    }

    /// <summary>
    /// Sends the agent to the back of the queue deterministically: rather than hoping an agility penalty
    /// will be enough, it assigns it an AV beyond the maximum currently in the queue.
    ///
    /// Known limitation: if the agent is executing its own turn, <see cref="CompleteActiveTurn"/>
    /// recomputes its CurrentAV at the end of the turn anyway and the effect is lost. In the real flow,
    /// whoever calls this method (a part breaking) acts during somebody else's turn.
    /// </summary>
    public void SendToBack(ITurnAgent agent)
    {
        var state = _turnQueue.Find(s => s.Agent == agent);
        if (state == null) return;

        float maxAV = 0f;
        for (int i = 0; i < _turnQueue.Count; i++)
            if (_turnQueue[i] != state) maxAV = Mathf.Max(maxAV, _turnQueue[i].CurrentAV);

        state.CurrentAV = maxAV + CalculateBaseAV(agent);
        SortQueue();
        _onQueueUpdated?.RaiseEvent();
    }

    private float CalculateBaseAV(ITurnAgent a)
    {
        float speed = Mathf.Max(1, a.EffectiveAgility);
        return 10_000f / speed;
    }

    private void SortQueue()
    {
        _turnQueue.Sort((a, b) => {
            int result = a.CurrentAV.CompareTo(b.CurrentAV);

            if (result == 0)
                return b.Agent.EffectiveAgility.CompareTo(a.Agent.EffectiveAgility);

            return result;
        });
    }
}
