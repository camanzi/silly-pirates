using AYellowpaper.SerializedCollections;
using UnityEngine;

[CreateAssetMenu(fileName = "CameraCueDefaultProfiles", menuName = "Camera/Camera Cue Default Profiles")]
public class CameraCueDefaultProfilesSO : ScriptableObject
{
    [SerializeField] private SerializedDictionary<CameraCueType, CameraCueProfileSO> _profilesByCueType;
    [SerializeField] private CameraCueProfileSO _fallbackProfile;

    public CameraCueProfileSO GetProfile(CameraCueType type)
    {
        if (_profilesByCueType.TryGetValue(type, out var profile) && profile != null) return profile;
        return _fallbackProfile;
    }
}
