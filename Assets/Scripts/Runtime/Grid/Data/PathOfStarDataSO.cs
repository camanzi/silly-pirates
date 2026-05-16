using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Path Of Star Data", menuName = "Grid/Path Of Star Data")]
public class PathOfStarDataSO : ScriptableObject
{
    [Header("Dependencies")]
    [SerializeField] private TurnAgentEventChannel _onAnyTurnStarted;
    [SerializeField] private TurnOrderDataSO _turnOrderData;
    [SerializeField] private GridStateDataSO _gridStateData;
    [SerializeField] private HighlightGridEventChannel _highlightChannel;

    [Header("Config")]
    [SerializeField] private float _agilityBonusPerApplication = 2f;
    [SerializeField] private int _cellDurationInTurns = 3;

    private readonly Dictionary<Vector3Int, int> _cellCountdowns = new();
    private readonly Dictionary<ITurnAgent, StarAgilityBonusPassiveSO> _bonusPassives = new();
    private readonly List<Vector3Int> _toRemove = new();

    private void OnEnable()
    {
        _cellCountdowns.Clear();
        _bonusPassives.Clear();
        if (_onAnyTurnStarted != null)
            _onAnyTurnStarted.OnEventRaised += OnTurnStarted;
    }

    private void OnDisable()
    {
        if (_onAnyTurnStarted != null)
            _onAnyTurnStarted.OnEventRaised -= OnTurnStarted;
    }

    public void Apply(Vector3Int cell)
    {
        _cellCountdowns[cell] = _cellDurationInTurns;

        var entities = _gridStateData.GetEntityAt(cell);
        if (entities != null)
        {
            foreach (var entity in entities)
            {
                if (entity is IAbilityHolder holder && entity is ITurnAgent agent)
                    ApplyBonus(holder, agent);
            }
        }

        RaiseHighlightEvent();
    }

    private void OnTurnStarted(ITurnAgent startingAgent)
    {
        if (startingAgent is IAbilityHolder startHolder)
            ResetBonus(startHolder, startingAgent);

        _toRemove.Clear();
        var cellKeys = new List<Vector3Int>(_cellCountdowns.Keys);
        foreach (var cell in cellKeys)
        {
            _cellCountdowns[cell]--;
            if (_cellCountdowns[cell] <= 0) _toRemove.Add(cell);
        }
        foreach (var cell in _toRemove)
            _cellCountdowns.Remove(cell);

        foreach (var cell in _cellCountdowns.Keys)
        {
            var entities = _gridStateData.GetEntityAt(cell);
            if (entities == null) continue;
            foreach (var entity in entities)
            {
                if (entity is IAbilityHolder holder && entity is ITurnAgent agent && agent != startingAgent)
                    ApplyBonus(holder, agent);
            }
        }

        RaiseHighlightEvent();
    }

    private void ApplyBonus(IAbilityHolder holder, ITurnAgent agent)
    {
        if (!_bonusPassives.TryGetValue(agent, out var bonus))
        {
            bonus = CreateInstance<StarAgilityBonusPassiveSO>();
            holder.PassiveAbilityController.AddPassive(bonus);
            _bonusPassives[agent] = bonus;
        }
        bonus.PercentageBonus += _agilityBonusPerApplication;
        _turnOrderData.UpdateAgentAV(agent);
    }

    private void ResetBonus(IAbilityHolder holder, ITurnAgent agent)
    {
        if (!_bonusPassives.TryGetValue(agent, out var bonus)) return;
        holder.PassiveAbilityController.RemovePassive(bonus);
        _bonusPassives.Remove(agent);
        _turnOrderData.UpdateAgentAV(agent);
    }

    private void RaiseHighlightEvent()
    {
        if (_highlightChannel == null) return;
        var cells = new List<Vector3Int>(_cellCountdowns.Keys);
        _highlightChannel.RaiseEvent(HighlightGridPayload.PathOfStarUpdate(cells));
    }
}
