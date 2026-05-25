using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Path Of Star Data", menuName = "Grid/Path Of Star Data")]
public class PathOfStarDataSO : ScriptableObject
{
    [Header("Dependencies")]
    [SerializeField] private TurnAgentEventChannel _onAnyTurnEnded;
    [SerializeField] private GridStateDataSO _gridStateData;
    [SerializeField] private CellEffectEventChannel _effectChannel;

    [Header("Visual")]
    [SerializeField] private Material _material;

    [Header("Config")]
    [SerializeField] private float _avDiscountPercentage = 20f;
    [SerializeField] private int _cellDurationInTurns = 3;

    private readonly Dictionary<Vector3Int, int> _cellCountdowns = new();
    private readonly List<Vector3Int> _toRemove = new();

    private void OnEnable()
    {
        _cellCountdowns.Clear();
        if (_onAnyTurnEnded != null)
            _onAnyTurnEnded.OnEventRaised += OnTurnEnded;
    }

    private void OnDisable()
    {
        if (_onAnyTurnEnded != null)
            _onAnyTurnEnded.OnEventRaised -= OnTurnEnded;
        RaiseEffectEvent(null);
    }

    public void Apply(Vector3Int cell)
    {
        _cellCountdowns[cell] = _cellDurationInTurns;
        RaiseEffectEvent(new List<Vector3Int>(_cellCountdowns.Keys));
    }

    private void OnTurnEnded(ITurnAgent agent)
    {
        _toRemove.Clear();
        var cellKeys = new List<Vector3Int>(_cellCountdowns.Keys);
        foreach (var cell in cellKeys)
        {
            _cellCountdowns[cell]--;
            if (_cellCountdowns[cell] <= 0) _toRemove.Add(cell);
        }
        foreach (var cell in _toRemove)
            _cellCountdowns.Remove(cell);

        bool agentInStarCell = false;
        GridElement agentElement = agent as GridElement;
        if (agentElement != null)
        {
            foreach (var cell in _cellCountdowns.Keys)
            {
                var entities = _gridStateData.GetEntityAt(cell);
                if (entities == null) continue;
                if (entities.Contains(agentElement))
                {
                    agentInStarCell = true;
                    break;
                }
            }
        }

        if (agentInStarCell && agent is IAbilityHolder holder)
        {
            var bonus = CreateInstance<StarNextActionPassiveSO>();
            bonus.DiscountPercentage = _avDiscountPercentage;
            holder.PassiveAbilityController.AddPassive(bonus);
        }

        RaiseEffectEvent(new List<Vector3Int>(_cellCountdowns.Keys));
    }

    private void RaiseEffectEvent(List<Vector3Int> cells) =>
        _effectChannel?.RaiseEvent(new CellEffectPayload
        {
            Key = "star-path",
            Cells = cells,
            Material = _material
        });
}
