using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SpawnAllies Ability", menuName = "Abilities/Enemy/Spawn Allies Ability")]
public class SpawnAlliesAbility : EnemyAbilityBase
{
    [Header("Spawn Allies configs")]
    [SerializeField] private SpawnPointManagerSO _spawnPointManager;
    [SerializeField] private List<HostileCharacter> _allyPool;
    [SerializeField] private int _maxSpawns = 2;
    [Tooltip("Full turns with different abilities needed before this ability can be used again.")]
    [SerializeField] private int _cooldownTurns = 3;

    protected override bool MeetsPreconditions(AIContext context)
    {
        if (!base.MeetsPreconditions(context)) return false;
        return !context.Caster.AbilityCooldowns.TryGetValue(this, out int cd) || cd == 0;
    }

    protected override float ComputeScore(AIContext context, out TargetingData targeting)
    {
        targeting = default;
        if (_allyPool == null || _allyPool.Count == 0) return float.NegativeInfinity;
        if (!_spawnPointManager.HasFreeSpawnPoints()) return float.NegativeInfinity;
        return 1f;
    }

    public override bool CanExecute(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        return _allyPool != null && _allyPool.Count > 0 && _spawnPointManager.HasFreeSpawnPoints();
    }

    public override ICommand CreateCommand(IInteractableElement caster, TargetingData? targetingData, ref object cache)
    {
        var hostile = (HostileCharacter)caster;
        hostile.AbilityCooldowns[this] = _cooldownTurns + 1;

        var shuffled = new List<HostileCharacter>(_allyPool);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        int count = Mathf.Min(_maxSpawns, shuffled.Count);
        var freePoints = _spawnPointManager.GetFreeSpawnPoints(count, hostile.Transform.position);
        int pairCount = Mathf.Min(count, freePoints.Count);

        var pairs = new List<(HostileCharacter prefab, SpawnPoint point)>();
        for (int i = 0; i < pairCount; i++)
            pairs.Add((shuffled[i], freePoints[i]));

        Transform partTransform = GetRequiredPartTransform(caster);
        return new SpawnAlliesCommand(hostile, pairs, partTransform);
    }

    public override bool RequiresTargeting => false;
}
