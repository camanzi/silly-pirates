using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private VFXController _impactVFXPrefab;

    [Header("Audio")]
    [SerializeField] private SoundEventSO _impactSfx;
    [SerializeField] private SfxCueEventChannel _sfxChannel;

    public void PlayImpactEffect()
    {
        if (_impactVFXPrefab != null)
            Object.Instantiate(_impactVFXPrefab, transform.position, transform.rotation);

        if (_impactSfx != null && _sfxChannel != null)
            _sfxChannel.RaiseEvent(SfxCue.At(_impactSfx, transform.position));
    }
}
