using UnityEngine;

[CreateAssetMenu(fileName = "SlimeBombing Strike Step", menuName = "Abilities/Enemy/Steps/SlimeBombing Strike")]
public class SlimeBombingStrikeStepSO : MultiStepAbilityStepSO
{
    [Header("SlimeBombing Configs")]
    [SerializeField] private GameObject _projectile;
    [SerializeField] private int _baseDamage = 20;
    [SerializeField] private DamageType _damageType = DamageType.Physical;
    [SerializeField] private SlimyCellDataSO _slimyCellData;
    [SerializeField] private GridStateDataSO _gridStateData;
    [SerializeField] private TrajectoryConfigsSO _trajectoryConfig;

    [Header("Camera Direction (Impact Reveal)")]
    [SerializeField] private AbilityExecutionCueEventChannel _cameraCueChannel;
    [SerializeField] private CameraDirectorStateSO _cameraDirectorState;

    [Header("Volley Timing")]
    [SerializeField] private float _extraHeightPerExtraShot = 1.5f;
    [SerializeField] private float _extraDurationPerExtraShot = 0.4f;

    // Never actually invoked by MultiStepAbility today (mid-sequence scoring short-circuits to 100f);
    // implemented for interface completeness / future reuse.
    public override float ComputeScore(AIContext context, StepState previousState, out TargetingData targeting)
    {
        targeting = TargetingData.Empty;
        return previousState is { Zones: { Count: > 0 } } ? previousState.Zones.Count : float.NegativeInfinity;
    }

    public override ICommand CreateCommand(HostileCharacter caster, StepState state)
    {
        var channel = state.Extra.TryGetValue(ThreatCellEffectChannelKey, out var chObj) ? chObj as CellEffectEventChannel : null;
        var threatKey = state.Extra.TryGetValue(ThreatKeyKey, out var keyObj) ? keyObj as string : null;
        var ability = state.Extra.TryGetValue(OwningAbilityKey, out var abObj) ? abObj as AbilityBase : null;

        return new SlimeBombingCommand(
            state.Zones, _baseDamage, _damageType,
            _slimyCellData, _gridStateData,
            channel, threatKey,
            _projectile, _trajectoryConfig,
            caster, caster.CritStats, state,
            ability, _cameraCueChannel, _cameraDirectorState,
            _extraHeightPerExtraShot, _extraDurationPerExtraShot);
    }
}
