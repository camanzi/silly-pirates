using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

public class SlimeBombingCommand : ICommand
{
    private readonly List<(Vector3 Center, List<Vector3Int> Cells)> _zones;
    private readonly int _damage;
    private readonly DamageType _damageType;
    private readonly SlimyCellDataSO _slimyCellData;
    private readonly GridStateDataSO _gridStateData;
    private readonly CellEffectEventChannel _cellEffectChannel;
    private readonly string _threatKey;
    private readonly GameObject _projectilePrefab;
    private readonly TrajectoryConfigsSO _trajectoryConfig;
    private readonly HostileCharacter _caster;
    private readonly EnemyCritStatsSO _critStats;
    private readonly SlimeBombingAbility.SlimeBombingState _state;

    private readonly AbilityBase _ability;
    private readonly AbilityExecutionCueEventChannel _cameraCueChannel;
    private readonly CameraDirectorStateSO _cameraDirectorState;
    private readonly float _extraHeightPerExtraShot;
    private readonly float _extraDurationPerExtraShot;

    private static readonly Vector3 ProjectileScale = new(0.8f, 0.8f, 1.4f);

    public SlimeBombingCommand(
        List<(Vector3 Center, List<Vector3Int> Cells)> zones,
        int damage, DamageType damageType,
        SlimyCellDataSO slimyCellData, GridStateDataSO gridStateData,
        CellEffectEventChannel cellEffectChannel, string threatKey,
        GameObject projectilePrefab, TrajectoryConfigsSO trajectoryConfig,
        HostileCharacter caster, EnemyCritStatsSO critStats,
        SlimeBombingAbility.SlimeBombingState state = null,
        AbilityBase ability = null,
        AbilityExecutionCueEventChannel cameraCueChannel = null,
        CameraDirectorStateSO cameraDirectorState = null,
        float extraHeightPerExtraShot = 0f,
        float extraDurationPerExtraShot = 0f)
    {
        _zones = zones;
        _damage = damage;
        _damageType = damageType;
        _slimyCellData = slimyCellData;
        _gridStateData = gridStateData;
        _cellEffectChannel = cellEffectChannel;
        _threatKey = threatKey;
        _projectilePrefab = projectilePrefab;
        _trajectoryConfig = trajectoryConfig;
        _caster = caster;
        _critStats = critStats;
        _state = state;
        _ability = ability;
        _cameraCueChannel = cameraCueChannel;
        _cameraDirectorState = cameraDirectorState;
        _extraHeightPerExtraShot = extraHeightPerExtraShot;
        _extraDurationPerExtraShot = extraDurationPerExtraShot;
    }

    public async Awaitable ExecuteAsync()
    {
        var t = _caster.Transform;

        if (_state != null)
        {
            if (_state.ActiveShakeTween.isAlive) _state.ActiveShakeTween.Stop();
            if (_state.PartTransform != null && _state.PartOriginalScale != Vector3.zero)
                await Tween.Scale(_state.PartTransform, _state.PartOriginalScale, 0.3f, Ease.InOutQuad);
        }

        var original = t.localScale;

        // Phase 1: the boss fires all N shots in sequence while the camera stays parked on it
        // (held there by the turn-start FocusCaster cue). Projectiles are launched but not
        // awaited here, so every shot is visibly fired before the camera moves to phase 2.
        var flights = new List<Awaitable>(_zones.Count);
        for (int i = 0; i < _zones.Count; i++)
        {
            await Tween.Scale(t, original * 0.6f, 0.15f, Ease.InQuad);
            await Tween.Scale(t, original * 1.2f, 0.10f, Ease.OutQuad);
            await Tween.Scale(t, original,        0.10f, Ease.InOutQuad);

            float extraHeight   = i == 0 ? 0f : _extraHeightPerExtraShot;
            float extraDuration = i == 0 ? 0f : _extraDurationPerExtraShot;
            flights.Add(LaunchProjectileAt(_zones[i].Center, _zones[i].Cells, extraHeight, extraDuration));
        }

        // Phase 2: live impact reveal — anticipate each landing in firing order so the camera is
        // already framing the zone when its (already in-flight) projectile lands.
        for (int i = 0; i < _zones.Count; i++)
        {
            if (_cameraDirectorState != null && _cameraCueChannel != null)
            {
                var cue = new AbilityExecutionCue(_ability, _caster, null, null, _zones[i].Center)
                {
                    CueTypeOverride = CameraCueType.FocusArea
                };
                await _cameraDirectorState.RaiseCueAndWaitAsync(_cameraCueChannel, cue);
            }

            await flights[i];
        }

        _cellEffectChannel?.RaiseEvent(new CellEffectPayload { Key = _threatKey, Cells = null });
    }

    private async Awaitable LaunchProjectileAt(Vector3 target, List<Vector3Int> cells, float extraHeight, float extraDuration)
    {
        var projectile = GameObject.Instantiate(_projectilePrefab, _caster.Transform.position, Quaternion.identity);

        Vector3 start = _caster.Transform.position;
        Vector3 ctrl  = (start + target) / 2f + Vector3.up * (_trajectoryConfig.Height + extraHeight);

        var state = new ProjectileState(projectile.transform, start, ctrl, target);
        await Tween.Custom(state, 0f, 1f, duration: _trajectoryConfig.TravelDuration + extraDuration, ease: Ease.Linear,
            onValueChange: static (s, progress) =>
            {
                Vector3 cur = MathUtils.EvaluateBezierPoint(progress, s.Start, s.ControlPoint, s.End);
                s.Projectile.LookAt(MathUtils.EvaluateBezierPoint(Mathf.Min(progress + 0.01f, 1f), s.Start, s.ControlPoint, s.End));
                s.Projectile.position   = cur;
                s.Projectile.localScale = ProjectileScale;
            });

        ApplyDamageAndSlimy(cells);
        GameObject.Destroy(projectile);
    }

    private void ApplyDamageAndSlimy(List<Vector3Int> cells)
    {
        foreach (var cell in cells)
        {
            var entities = _gridStateData?.GetEntityAt(cell);
            if (entities != null)
            {
                foreach (var entity in entities)
                {
                    if (entity is not IHealthOwner ho || !ho.Health.IsAlive) continue;

                    float dmg   = _damage;
                    bool isCrit = false;
                    if (_critStats != null)
                    {
                        isCrit = UnityEngine.Random.Range(0, 100) < _critStats.CritRate;
                        if (isCrit) dmg += dmg * _critStats.CritDMG / 100f;
                    }
                    ho.Health.TakeDamage(new DamagePayload(dmg, _damageType) { IsCritical = isCrit });
                }
            }

            _slimyCellData?.Apply(cell);
        }
    }

    public void Undo() { }

    private class ProjectileState
    {
        public readonly Transform Projectile;
        public readonly Vector3 Start, ControlPoint, End;

        public ProjectileState(Transform p, Vector3 s, Vector3 cp, Vector3 e)
        {
            Projectile   = p;
            Start        = s;
            ControlPoint = cp;
            End          = e;
        }
    }
}
