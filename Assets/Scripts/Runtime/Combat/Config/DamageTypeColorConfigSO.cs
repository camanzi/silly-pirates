using AYellowpaper.SerializedCollections;
using UnityEngine;

[CreateAssetMenu(fileName = "DamageTypeColorConfig", menuName = "Combat/Damage Type Color Config")]
public class DamageTypeColorConfigSO : ScriptableObject
{
    [SerializeField] private SerializedDictionary<DamageType, Color> _colorsByType;
    [SerializeField] private Color _fallbackColor = Color.white;

    public Color GetColor(DamageType type)
    {
        if (_colorsByType.TryGetValue(type, out var color)) return color;
        return _fallbackColor;
    }
}
