using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Slimy Cell Data", menuName = "Grid/Slimy Cell Data")]
public class SlimyCellDataSO : ScriptableObject, ICellCostModifier
{
    [Header("Dependencies")]
    [SerializeField] private TurnAgentEventChannel _onAnyTurnEnded;
    [SerializeField] private CellEffectEventChannel _effectChannel;
    [SerializeField] private CellCostRegistrySO _cellCostRegistry;

    [Header("Visual")]
    [SerializeField] private Material _material;

    [Header("Config")]
    [SerializeField] private int _additionalMoveCost = 1;
    [SerializeField] private int _cellDurationInTurns = 3;

    private readonly Dictionary<Vector3Int, int> _cellCountdowns = new();
    private readonly List<Vector3Int> _toRemove = new();

    private void OnEnable()
    {
        _cellCountdowns.Clear();
        _cellCostRegistry?.Register(this);
        if (_onAnyTurnEnded != null)
            _onAnyTurnEnded.OnEventRaised += OnTurnEnded;
    }

    private void OnDisable()
    {
        _cellCostRegistry?.Unregister(this);
        if (_onAnyTurnEnded != null)
            _onAnyTurnEnded.OnEventRaised -= OnTurnEnded;
        RaiseEffectEvent(null);
    }

    public void Apply(Vector3Int cell)
    {
        _cellCountdowns[cell] = _cellDurationInTurns;
        RaiseEffectEvent(new List<Vector3Int>(_cellCountdowns.Keys));
    }

    public int GetAdditionalCost(Vector3Int cell) =>
        _cellCountdowns.ContainsKey(cell) ? _additionalMoveCost : 0;

    private void OnTurnEnded(ITurnAgent agent)
    {
        _toRemove.Clear();
        var keys = new List<Vector3Int>(_cellCountdowns.Keys);
        foreach (var cell in keys)
        {
            _cellCountdowns[cell]--;
            if (_cellCountdowns[cell] <= 0)
                _toRemove.Add(cell);
        }
        foreach (var cell in _toRemove)
            _cellCountdowns.Remove(cell);

        RaiseEffectEvent(new List<Vector3Int>(_cellCountdowns.Keys));
    }

    private void RaiseEffectEvent(List<Vector3Int> cells) =>
        _effectChannel?.RaiseEvent(new CellEffectPayload
        {
            Key = "slimy",
            Cells = cells,
            Material = _material
        });
}
