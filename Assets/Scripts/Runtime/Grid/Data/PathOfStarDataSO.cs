using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Path Of Star Data", menuName = "Grid/Path Of Star Data")]
public class PathOfStarDataSO : ScriptableObject, ICellCostModifier
{
    [Header("Dependencies")]
    [SerializeField] private TurnAgentEventChannel _onAnyTurnEnded;
    [SerializeField] private CellEffectEventChannel _effectChannel;
    [SerializeField] private CellCostRegistrySO _cellCostRegistry;

    [Header("Visual")]
    [SerializeField] private Material _material;

    [Header("Config")]
    [SerializeField] private int _cellDurationInTurns = 3;

    private readonly Dictionary<Vector3Int, int> _cellCountdowns = new();
    private readonly List<Vector3Int> _toRemove = new();

    private void OnEnable()
    {
        _cellCountdowns.Clear();
        if (_onAnyTurnEnded != null)
            _onAnyTurnEnded.OnEventRaised += OnTurnEnded;
        _cellCostRegistry?.Register(this);
    }

    private void OnDisable()
    {
        if (_onAnyTurnEnded != null)
            _onAnyTurnEnded.OnEventRaised -= OnTurnEnded;
        _cellCostRegistry?.Unregister(this);
        RaiseEffectEvent(null);
    }

    public void Apply(Vector3Int cell)
    {
        _cellCountdowns[cell] = _cellDurationInTurns;
        RaiseEffectEvent(new List<Vector3Int>(_cellCountdowns.Keys));
    }

    public int GetAdditionalCost(Vector3Int cell) =>
        _cellCountdowns.ContainsKey(cell) ? -1 : 0;

    private void OnTurnEnded(ITurnAgent agent)
    {
        if (!agent.CompareTag("Player")) return;
        _toRemove.Clear();
        var cellKeys = new List<Vector3Int>(_cellCountdowns.Keys);
        foreach (var cell in cellKeys)
        {
            _cellCountdowns[cell]--;
            if (_cellCountdowns[cell] <= 0) _toRemove.Add(cell);
        }
        foreach (var cell in _toRemove)
            _cellCountdowns.Remove(cell);

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
