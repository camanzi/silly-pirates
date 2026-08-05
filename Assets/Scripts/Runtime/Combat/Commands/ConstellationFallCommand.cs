using UnityEngine;

public class ConstellationFallCommand : ICommand
{
    private readonly IInteractableElement _caster;
    private readonly Vector3Int _targetCell;
    private readonly Vector3 _worldPosition;
    private readonly ConstellationFragment _piecePrefab;
    private readonly int _apCost;
    private GameObject _spawnedPiece;

    public ConstellationFallCommand(IInteractableElement caster, Vector3Int targetCell, Vector3 worldPosition, ConstellationFragment piecePrefab, int apCost)
    {
        _caster = caster;
        _targetCell = targetCell;
        _worldPosition = worldPosition;
        _piecePrefab = piecePrefab;
        _apCost = apCost;
    }

    public async Awaitable ExecuteAsync()
    {
        if (_caster is ITurnAgent turnAgent)
            turnAgent.RemainingActionPoints -= _apCost;

        _spawnedPiece = GameObject.Instantiate(_piecePrefab.gameObject, _worldPosition, Quaternion.identity);

        // Wait out the fragment's own fall so the turn doesn't end (and the action camera doesn't release)
        // before the payoff lands. Read from the prefab, not the instance: an early pickup destroys it mid-fall.
        await Awaitable.WaitForSecondsAsync(_piecePrefab.FallDuration);
    }

    public void Undo()
    {
        if (_caster is ITurnAgent turnAgent)
            turnAgent.RemainingActionPoints += _apCost;

        if (_spawnedPiece != null)
            GameObject.Destroy(_spawnedPiece);
    }
}
