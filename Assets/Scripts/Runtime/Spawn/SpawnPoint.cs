using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private SpawnPointManagerSO _manager;
    [SerializeField] private float _claimRadius = 0.4f;

    private HostileCharacter _occupant;

    public bool IsOccupied => _occupant != null;
    public Vector3 Position => transform.position;
    public HostileCharacter Occupant => _occupant;

    private void Awake()
    {
        var col = GetComponent<SphereCollider>();
        col.isTrigger = true;
    }

    private void OnEnable() => _manager.Register(this);

    private void Start()
    {
        var hits = Physics.OverlapSphere(transform.position, _claimRadius);
        foreach (var col in hits)
        {
            var hostile = col.GetComponent<HostileCharacter>();
            if (hostile != null)
            {
                Claim(hostile);
                return;
            }
        }
    }

    private void OnDisable() => _manager.Unregister(this);

    private void OnTriggerEnter(Collider other)
    {
        if (_occupant != null) return;
        var hostile = other.GetComponent<HostileCharacter>();
        if (hostile != null) Claim(hostile);
    }

    public void Claim(HostileCharacter hostile)
    {
        _occupant = hostile;
        hostile.SetSpawnPoint(this);
    }

    public void Release() => _occupant = null;
}
