using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private VFXController _impactVFXPrefab;

    public void PlayImpactEffect()
    {
        if (_impactVFXPrefab != null)
            Object.Instantiate(_impactVFXPrefab, transform.position, transform.rotation);
    }
}
