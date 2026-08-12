using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private VFXController _impactVFXPrefab;
    [SerializeField] private VfxCueEventChannel _vfxChannel;

    [Header("Audio")]
    [SerializeField] private SoundEventSO _impactSfx;
    [SerializeField] private SfxCueEventChannel _sfxChannel;

    public void PlayImpactEffect()
    {
        if (_impactVFXPrefab != null && _vfxChannel != null)
            _vfxChannel.RaiseEvent(VfxCue.At(_impactVFXPrefab, transform.position, transform.rotation));

        if (_impactSfx != null && _sfxChannel != null)
            _sfxChannel.RaiseEvent(SfxCue.At(_impactSfx, transform.position));
    }
}
