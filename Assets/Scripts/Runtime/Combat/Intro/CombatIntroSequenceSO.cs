using UnityEngine;

/// <summary>
/// Tuning della sequenza di intro al combattimento. Nessuno stato runtime: solo dati di
/// configurazione condivisibili tra scene.
/// </summary>
[CreateAssetMenu(fileName = "CombatIntroSequence", menuName = "Combat/Intro/Combat Intro Sequence")]
public class CombatIntroSequenceSO : ScriptableObject
{
    [Header("Timing")]
    [Tooltip("Attesa prima del primo beat di camera, dopo che gli spawn point hanno rivendicato i loro occupanti")]
    [Min(0f)]
    [SerializeField] private float _preSequenceDelay = 0.25f;

    [Tooltip("Hold della camera sullo spawn point PRIMA che il nemico emerga")]
    [Min(0f)]
    [SerializeField] private float _perEnemyPreHold = 0.3f;

    [Tooltip("Hold della camera sullo spawn point DOPO che il nemico è emerso")]
    [Min(0f)]
    [SerializeField] private float _perEnemyPostHold = 0.4f;

    [Tooltip("Hold della camera sul punto di attracco DOPO che la nave si è fermata")]
    [Min(0f)]
    [SerializeField] private float _shipArrivalHold = 0.6f;

    [Tooltip("Attesa dopo l'attracco, prima di sbloccare il turn loop e mostrare la HUD")]
    [Min(0f)]
    [SerializeField] private float _postDockDelay = 0.2f;

    [Header("Testo")]
    [SerializeField] private string _openingLine = "Il combattimento ha inizio!";

    [Header("Camera")]
    [SerializeField] private CameraCueProfileSO _enemyFocusProfile;
    [SerializeField] private CameraCueProfileSO _shipFocusProfile;

    public float PreSequenceDelay => _preSequenceDelay;
    public float PerEnemyPreHold => _perEnemyPreHold;
    public float PerEnemyPostHold => _perEnemyPostHold;
    public float ShipArrivalHold => _shipArrivalHold;
    public float PostDockDelay => _postDockDelay;
    public string OpeningLine => _openingLine;
    public CameraCueProfileSO EnemyFocusProfile => _enemyFocusProfile;
    public CameraCueProfileSO ShipFocusProfile => _shipFocusProfile;
}
