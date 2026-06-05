using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SpawnPointManager", menuName = "Combat/Spawn/Spawn Point Manager")]
public class SpawnPointManagerSO : ScriptableObject
{
    private readonly List<SpawnPoint> _registeredPoints = new();

    private void OnEnable() => _registeredPoints.Clear();

    public void Register(SpawnPoint point)
    {
        if (!_registeredPoints.Contains(point))
            _registeredPoints.Add(point);
    }

    public void Unregister(SpawnPoint point) => _registeredPoints.Remove(point);

    public bool HasFreeSpawnPoints()
    {
        foreach (var p in _registeredPoints)
            if (!p.IsOccupied) return true;
        return false;
    }

    public List<SpawnPoint> GetFreeSpawnPoints(int count, Vector3 origin)
    {
        var free = new List<SpawnPoint>();
        foreach (var p in _registeredPoints)
            if (!p.IsOccupied) free.Add(p);

        free.Sort((a, b) =>
            Vector3.SqrMagnitude(a.Position - origin)
                .CompareTo(Vector3.SqrMagnitude(b.Position - origin)));

        if (free.Count > count)
            free.RemoveRange(count, free.Count - count);
        return free;
    }
}
