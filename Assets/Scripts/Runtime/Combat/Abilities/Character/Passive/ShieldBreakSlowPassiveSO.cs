using UnityEngine;

/// <summary>
/// Punizione inflitta a chi si vede rompere lo scudo elementale: l'agente perde il posto in coda e
/// finisce ultimo. La penalita' di agilita' e' a zero di default perche' il colpo e' il salto in coda,
/// non un debuff prolungato; resta serializzata per il tuning.
///
/// Con penalita' a zero <c>HostileCharacter.HandlePassivesChanged</c> non emette alcun AVDelta, che
/// altrimenti si sommerebbe al SendToBack falsandolo.
///
/// Classe a se' e non un asset di <see cref="SlowPassiveSO"/>: PassiveAbilityController.AddPassive
/// deduplica per GetType() esatto, quindi un secondo SlowPassiveSO si stackerebbe con "Entangled".
///
/// Con durata 0 e RemovalTiming.OwnerTurnStart la passiva sparisce al turno successivo dell'owner,
/// in sincrono con la rigenerazione dello scudo.
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
