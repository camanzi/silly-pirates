using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Slimy Curse Passive", menuName = "Abilities/Character/Passives/Slimy Curse")]
public class SlimyCursePassiveSO : PassiveAbilitySO, IOnCellEntered, IOnTurnStart, ICombatSessionResettable
{
    [SerializeField] private SlimyCellDataSO _slimyCellData;
    [SerializeField] private int _durationInTurns = 1;
    [SerializeField] private VFXController _vfxPrefab;
    [SerializeField] private VfxCueEventChannel _vfxChannel;
    [SerializeField] private VfxStopEventChannel _vfxStopChannel;

    private static readonly HashSet<GridCharacter> _activeCurseTargets = new();
    private PassiveAbilityController _controller;
    private int _turnStartCount;
    private VfxHandle _vfxHandle;

    public static bool IsActiveOn(GridCharacter character) => _activeCurseTargets.Contains(character);

    public override void OnEquip(PassiveAbilityController controller)
    {
        _controller = controller;
        _turnStartCount = 0;
        if (controller.TryGetComponent<GridCharacter>(out var character))
            _activeCurseTargets.Add(character);
        // Il VFX insegue il personaggio invece di essergli parentato: un oggetto del pool
        // non va mai riparentato su un oggetto di gameplay.
        if (_vfxPrefab != null && _vfxChannel != null && !_vfxHandle.IsValid)
        {
            _vfxHandle = VfxHandle.New();
            _vfxChannel.RaiseEvent(VfxCue.Persistent(_vfxPrefab, controller.transform, _vfxHandle));
        }
    }

    public override void OnUnequip(PassiveAbilityController controller)
    {
        if (controller.TryGetComponent<GridCharacter>(out var character))
            _activeCurseTargets.Remove(character);
        if (_vfxHandle.IsValid && _vfxStopChannel != null)
            _vfxStopChannel.RaiseEvent(_vfxHandle);
        _vfxHandle = VfxHandle.None;

        _controller = null;
    }

    // HashSet statico condiviso fra tutti i cloni: senza reset, i personaggi del combattimento
    // precedente (distrutti) restano dentro e IsActiveOn continuerebbe a segnalarli come maledetti.
    public void ResetForNewCombat() => _activeCurseTargets.Clear();

    void IOnCellEntered.OnCellEntered(Vector3Int cell) => _slimyCellData?.Apply(cell);

    void IOnTurnStart.OnTurnStart()
    {
        _turnStartCount++;
        if (_turnStartCount > _durationInTurns)
            _controller.RemovePassive(this);
    }
}
