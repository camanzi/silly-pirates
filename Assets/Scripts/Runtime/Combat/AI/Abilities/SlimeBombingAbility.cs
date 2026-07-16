using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

[CreateAssetMenu(fileName = "SlimeBombing Ability", menuName = "Abilities/Enemy/Slime Bombing Ability")]
public class SlimeBombingAbility : TelegraphedEnemyAbility
{
    [Header("SlimeBombing Configs")]
    [SerializeField] private GameObject _projectile;
    [SerializeField] private int _baseDamage = 20;
    [SerializeField] private DamageType _damageType = DamageType.Physical;
    [SerializeField] private SlimyCellDataSO _slimyCellData;

    [Header("Camera Direction (Impact Reveal)")]
    [SerializeField] private AbilityExecutionCueEventChannel _cameraCueChannel;
    [SerializeField] private CameraDirectorStateSO _cameraDirectorState;

    [Header("Volley Timing")]
    [SerializeField] private float _extraHeightPerExtraShot = 1.5f;
    [SerializeField] private float _extraDurationPerExtraShot = 0.4f;

    protected override string ThreatKey => "slime-bombing";

    public sealed class SlimeBombingState : TelegraphState
    {
        public List<(Vector3 Center, List<Vector3Int> Cells)> Zones;
        public Tween ActiveShakeTween;
        public Vector3 CasterOriginalScale;
        public Vector3 PartOriginalScale;
        public Transform PartTransform;
    }

    protected override float ComputeTelegraph(AIContext context, out TargetingData targeting, out TelegraphState state)
    {
        targeting = TargetingData.Empty;
        state = null;

        var candidates = new List<(GridCharacter gc, ITargettable t, float hp)>();

        foreach (var entry in context.TurnOrder.TurnQueue)
        {
            if (entry.Agent is HostileCharacter) continue;
            if (entry.Agent is not IHealthOwner ho || !ho.Health.IsAlive) continue;
            if (entry.Agent is not GridCharacter gc) continue;
            if (entry.Agent is not ITargettable t) continue;
            candidates.Add((gc, t, ho.Health.CurrentHp));
        }

        if (candidates.Count == 0)
            return float.NegativeInfinity;

        candidates.Sort((a, b) => b.hp.CompareTo(a.hp));

        int targetCount = Mathf.Min(2, candidates.Count);
        var shape = ShapeFactory.GetShape(ShapeType.Circle);
        var zones = new List<(Vector3 Center, List<Vector3Int> Cells)>(targetCount);
        var allCells = new HashSet<Vector3Int>();

        for (int i = 0; i < targetCount; i++)
        {
            var (gc, t, _) = candidates[i];
            Vector3 center = t.Transform.position;
            var rawCells = shape.GetCells(gc.gridPosition, 1, gc.gridPosition);
            var cells = rawCells.ConvertAll(v => Vector3Int.FloorToInt(v));
            zones.Add((center, cells));
            foreach (var c in cells) allCells.Add(c);
        }

        state = new SlimeBombingState
        {
            Zones              = zones,
            AllCells           = new List<Vector3Int>(allCells),
            AffectedWorldPoints = zones.ConvertAll(z => z.Center)
        };

        return targetCount;
    }

    protected override ICommand CreateTelegraphCommand(HostileCharacter caster, TelegraphState baseState)
    {
        var s = (SlimeBombingState)baseState;
        s.PartTransform = GetRequiredPartTransform(caster);
        return new SlimeBombingTelegraphCommand(
            baseState.AllCells, _cellEffectChannel, _threatMaterial, ThreatKey, s, caster);
    }

    protected override ICommand CreateStrikeCommand(HostileCharacter caster, TelegraphState baseState)
    {
        var s = (SlimeBombingState)baseState;
        return new SlimeBombingCommand(
            s.Zones, _baseDamage, _damageType,
            _slimyCellData, _gridStateData,
            _cellEffectChannel, ThreatKey,
            _projectile, trajectoryConfigData,
            caster, caster.CritStats, s,
            this, _cameraCueChannel, _cameraDirectorState,
            _extraHeightPerExtraShot, _extraDurationPerExtraShot);
    }

    protected override void OnPendingStateRolledBack(HostileCharacter caster, TelegraphState baseState)
    {
        var s = (SlimeBombingState)baseState;
        if (s.ActiveShakeTween.isAlive) s.ActiveShakeTween.Stop();
        if (caster != null && s.CasterOriginalScale != Vector3.zero)
            Tween.Scale(caster.Transform, s.CasterOriginalScale, 0.3f, Ease.InOutQuad);
        if (s.PartTransform != null && s.PartOriginalScale != Vector3.zero)
            Tween.Scale(s.PartTransform, s.PartOriginalScale, 0.3f, Ease.InOutQuad);
    }
}
