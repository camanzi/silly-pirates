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

    [Header("Audio")]
    [Tooltip("Musica della cinematica di presentazione: suona mentre la camera passa sui nemici e " +
        "la nave attracca, e si spegne quando parte il fight. Deve avere _loop = true e _is3D = false " +
        "(loop 2D non posizionale sul mixer Music) — il loop serve a poterla fermare in fade-out, non a ripeterla.")]
    [SerializeField] private SoundEventSO _introMusic;

    [Tooltip("Durata del fade-in della musica d'intro, in secondi")]
    [Min(0f)]
    [SerializeField] private float _introMusicFadeIn = 0.5f;

    [Tooltip("Durata del fade-out della musica d'intro quando parte il fight, in secondi")]
    [Min(0f)]
    [SerializeField] private float _introMusicFadeOut = 1f;

    [Tooltip("Soundtrack principale del combattimento: parte in concomitanza col banner \"The hunt is open!\" " +
        "e continua in loop per tutto lo scontro. Deve avere _loop = true e _is3D = false " +
        "(loop 2D non posizionale sul mixer Music).")]
    [SerializeField] private SoundEventSO _combatMusic;

    [Tooltip("Durata del fade-in della musica di combattimento, sul beat del banner")]
    [Min(0f)]
    [SerializeField] private float _combatMusicFadeIn = 0.5f;

    public float PreSequenceDelay => _preSequenceDelay;
    public float PerEnemyPreHold => _perEnemyPreHold;
    public float PerEnemyPostHold => _perEnemyPostHold;
    public float ShipArrivalHold => _shipArrivalHold;
    public float PostDockDelay => _postDockDelay;
    public string OpeningLine => _openingLine;
    public CameraCueProfileSO EnemyFocusProfile => _enemyFocusProfile;
    public CameraCueProfileSO ShipFocusProfile => _shipFocusProfile;
    public SoundEventSO IntroMusic => _introMusic;
    public float IntroMusicFadeIn => _introMusicFadeIn;
    public float IntroMusicFadeOut => _introMusicFadeOut;
    public SoundEventSO CombatMusic => _combatMusic;
    public float CombatMusicFadeIn => _combatMusicFadeIn;
}
