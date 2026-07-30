using UnityEngine;

[RequireComponent(typeof(HealthController))]
public class HealVFXFeedback : MonoBehaviour
{
    [SerializeField] private VFXController _healVFXPrefab;
    [SerializeField] private float _referenceRadius = 1.7f;
    [SerializeField] private float _fallbackRadius = 1.7f;
    [SerializeField] private float _minScale = 0.6f;
    [SerializeField] private float _maxScale = 2.2f;

    private HealthController _health;
    private Collider _collider;
    private bool _warnedMissingCollider;

    private void Awake()
    {
        _health = GetComponent<HealthController>();
        _collider = GetComponent<Collider>();
    }

    private void OnEnable()
    {
        _health.OnHealReceived += PlayHealVFX;
    }

    private void OnDisable()
    {
        _health.OnHealReceived -= PlayHealVFX;
    }

    private void PlayHealVFX()
    {
        if (_healVFXPrefab == null) return;

        Vector3 center;
        float radius;
        if (_collider != null)
        {
            Bounds bounds = _collider.bounds;
            center = bounds.center;
            radius = bounds.extents.magnitude;
        }
        else
        {
            if (!_warnedMissingCollider)
            {
                Debug.LogWarning($"HealVFXFeedback on '{name}' found no Collider — falling back to transform.position and a reference-sized radius.", this);
                _warnedMissingCollider = true;
            }
            center = transform.position + Vector3.up * _fallbackRadius;
            radius = _fallbackRadius;
        }

        VFXController instance = Object.Instantiate(_healVFXPrefab, center, Quaternion.identity);
        float scaleFactor = Mathf.Clamp(radius / _referenceRadius, _minScale, _maxScale);
        instance.transform.localScale = Vector3.one * scaleFactor;
    }
}
