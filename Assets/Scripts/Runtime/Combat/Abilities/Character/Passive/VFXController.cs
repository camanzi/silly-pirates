using UnityEngine;

public class VFXController : MonoBehaviour
{
    [SerializeField] private bool _autoDestroy = true;

    private ParticleSystem[] _particles;

    private void Awake()
    {
        _particles = GetComponentsInChildren<ParticleSystem>(true);
    }

    private void Start()
    {
        if (_autoDestroy)
            Destroy(gameObject, GetMaxParticleLifetime());
    }

    public void Release()
    {
        if (_autoDestroy)
            return;

        foreach (var ps in _particles)
        {
            var main = ps.main;
            main.loop = false;
        }

        foreach (var ps in _particles)
            ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);

        Destroy(gameObject, GetMaxParticleLifetime());
    }

    private float GetMaxParticleLifetime()
    {
        float max = 0f;
        foreach (var ps in _particles)
        {
            float lifetime = ps.main.startLifetime.constantMax;
            if (lifetime > max)
                max = lifetime;
        }
        return Mathf.Max(max, 0.1f);
    }
}
