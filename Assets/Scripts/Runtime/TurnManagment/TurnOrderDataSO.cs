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
    /// Varianza massima (frazione del AV base) applicata al primo AV di un agente il cui
    /// TurnAgentDataSO.RandomizeInitialAV è attivo. Modificare qui per cambiare quanta varietà
    /// ha l'ordine dei turni all'inizio del combattimento.
    /// </summary>
    public const float InitialAVVarianceRatio = 0.2f; // ±1/5 del AV base

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
            Debug.Log($"Ho aggiunto un nuovo Agent {mono.name} con AV: {initialAV}{(randomized ? " (randomizzato)" : "")}");

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
    /// Spedisce l'agente in fondo alla coda in modo deterministico: invece di sperare che una penalita' di
    /// agilita' basti, gli assegna un AV oltre il massimo attualmente in coda.
    ///
    /// Limite noto: se l'agente sta eseguendo il proprio turno, <see cref="CompleteActiveTurn"/> gli
    /// ricalcola comunque il CurrentAV a fine turno e l'effetto si perde. Nel flusso reale chi chiama
    /// questo metodo (rottura di una parte) agisce durante il turno di qualcun altro.
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
