using AYellowpaper.SerializedCollections;
using UnityEngine;

[CreateAssetMenu(fileName = "DamageTypeProjectileConfig", menuName = "Combat/Damage Type Projectile Config")]
public class DamageTypeProjectileConfigSO : ScriptableObject
{
    [SerializeField] private SerializedDictionary<DamageType, GameObject> _prefabsByType;
    [SerializeField] private GameObject _fallbackPrefab;

    public GameObject GetPrefab(DamageType type)
    {
        if (_prefabsByType.TryGetValue(type, out var prefab) && prefab != null)
            return prefab;
        return _fallbackPrefab;
    }
}
