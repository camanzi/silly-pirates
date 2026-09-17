using UnityEngine;

/// <summary>
/// The punishment inflicted on whoever gets their elemental shield broken: the agent loses its place in
/// the queue and ends up last. The agility penalty is zero by default because the blow IS the drop to the
/// back of the queue, not a lasting debuff; it stays serialized for tuning.
///
/// With a zero penalty <c>HostileCharacter.HandlePassivesChanged</c> emits no AVDelta, which would
/// otherwise add itself to the SendToBack and skew it.
///
/// A class of its own rather than an asset of <see cref="SlowPassiveSO"/>: PassiveAbilityController.AddPassive
/// deduplicates by exact GetType(), so a second SlowPassiveSO would stack with "Entangled".
///
/// With a duration of 0 and RemovalTiming.OwnerTurnStart the passive disappears on the owner's next turn,
/// in sync with the shield regenerating.
/// </summary>
[CreateAssetMenu(fileName = "Shield Break Slow Passive", menuName = "Abilities/Character/Passives/Shield Break Slow")]
public class ShieldBreakSlowPassiveSO : PassiveAbilitySO, IAgilityModifier, IOnGlobalTurnEnd, IOnTurnStart, IOnTurnEnd
{
    [Header("Shield break slow configs")]
    [SerializeField] private TurnOrderDataSO _turnOrderData;
    [SerializeField] private int _flatPenalty = 0;
    [SerializeField][Range(0, 100)] private float _percentPenalty = 0f;
    [SerializeField] private int _durationInTurns = 0;
    [SerializeField] private VFXController _vfxPrefab;
    [SerializeField] private VfxCueEventChannel _vfxChannel;

    private const float VfxReferenceRadius = 1.7f;
    private const float VfxMinScale = 0.6f;
    private const float VfxMaxScale = 2.2f;

    private PassiveAbilityController _controller;
    private int _turnCount;
    private bool _isExpired;

    public override void OnEquip(PassiveAbilityController controller)
    {
        _controller = controller;
        _turnCount  = 0;
        _isExpired  = false;

        var agent = controller.GetComponent<ITurnAgent>();
        if (_turnOrderData != null && agent != null) _turnOrderData.SendToBack(agent);

        PlayApplyVFX(controller);
    }

    private void PlayApplyVFX(PassiveAbilityController controller)
    {
        if (_vfxPrefab == null || _vfxChannel == null) return;

        Vector3 position = controller.transform.position;
        float scale = 1f;

        Collider collider = controller.Collider;
        if (collider != null)
        {
            Bounds bounds = collider.bounds;
            position = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
            scale = Mathf.Clamp(bounds.extents.magnitude / VfxReferenceRadius, VfxMinScale, VfxMaxScale);
        }

        _vfxChannel.RaiseEvent(VfxCue.At(_vfxPrefab, position, scale));
    }

    public override void OnUnequip(PassiveAbilityController controller)
    {
        _controller = null;
    }

    int IAgilityModifier.GetFlatAgilityBonus() => -_flatPenalty;

    float IAgilityModifier.GetPercentageAgilityBonus() => -_percentPenalty;

    void IOnGlobalTurnEnd.OnGlobalTurnEnd()
    {
        _turnCount++;
        if (_turnCount >= _durationInTurns)
        {
            if (RemovalTiming == PassiveRemovalTiming.AnyTurn)
                _controller.RemovePassive(this);
            else
                _isExpired = true;
        }
    }

    void IOnTurnStart.OnTurnStart()
    {
        if (_isExpired && RemovalTiming == PassiveRemovalTiming.OwnerTurnStart)
            _controller.RemovePassive(this);
    }

    void IOnTurnEnd.OnTurnEnd()
    {
        if (_isExpired && RemovalTiming == PassiveRemovalTiming.OwnerTurnEnd)
            _controller.RemovePassive(this);
    }
}
