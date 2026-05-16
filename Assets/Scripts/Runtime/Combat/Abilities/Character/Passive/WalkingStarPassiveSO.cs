using UnityEngine;

[CreateAssetMenu(fileName = "Walking Star Ability", menuName = "Abilities/Character/Passives/Walking Star")]
public class WalkingStarPassiveSO : PassiveAbilitySO, IOnCellEntered, IOnTurnStart
{
    [SerializeField] private PathOfStarDataSO _pathTracker;

    private PassiveAbilityController _controller;
    private int _turnStartCount;

    public override void OnEquip(PassiveAbilityController controller)
    {
        _controller = controller;
        _turnStartCount = 0;
    }

    public override void OnUnequip(PassiveAbilityController controller)
    {
        _controller = null;
    }

    void IOnCellEntered.OnCellEntered(Vector3Int cell) => _pathTracker.Apply(cell);

    void IOnTurnStart.OnTurnStart()
    {
        _turnStartCount++;
        if (_turnStartCount >= 2)
            _controller.RemovePassive(this);
    }
}
