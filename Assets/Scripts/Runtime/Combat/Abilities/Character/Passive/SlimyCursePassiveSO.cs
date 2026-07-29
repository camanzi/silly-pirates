using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Slimy Curse Passive", menuName = "Abilities/Character/Passives/Slimy Curse")]
public class SlimyCursePassiveSO : PassiveAbilitySO, IOnCellEntered, IOnTurnStart
{
    [SerializeField] private SlimyCellDataSO _slimyCellData;
    [SerializeField] private int _durationInTurns = 1;
    [SerializeField] private VFXController _vfxPrefab;

    private static readonly HashSet<GridCharacter> _activeCurseTargets = new();
    private PassiveAbilityController _controller;
    private int _turnStartCount;
    private VFXController _vfxInstance;

    public static bool IsActiveOn(GridCharacter character) => _activeCurseTargets.Contains(character);

    public override void OnEquip(PassiveAbilityController controller)
    {
        _controller = controller;
        _turnStartCount = 0;
        if (controller.TryGetComponent<GridCharacter>(out var character))
            _activeCurseTargets.Add(character);
        if (_vfxPrefab != null)
            _vfxInstance = Object.Instantiate(_vfxPrefab, controller.transform);
    }

    public override void OnUnequip(PassiveAbilityController controller)
    {
        if (controller.TryGetComponent<GridCharacter>(out var character))
            _activeCurseTargets.Remove(character);
        _vfxInstance?.Release();
        _vfxInstance = null;
        _controller = null;
    }

    void IOnCellEntered.OnCellEntered(Vector3Int cell) => _slimyCellData?.Apply(cell);

    void IOnTurnStart.OnTurnStart()
    {
        _turnStartCount++;
        if (_turnStartCount > _durationInTurns)
            _controller.RemovePassive(this);
    }
}
