using UnityEngine;

/// <summary>
/// Tuning for the combat intro sequence. No runtime state: configuration data only, shareable between
/// scenes.
/// </summary>
[CreateAssetMenu(fileName = "CombatIntroSequence", menuName = "Combat/Intro/Combat Intro Sequence")]
public class CombatIntroSequenceSO : ScriptableObject
{
    [Header("Timing")]
    [Tooltip("Wait before the first camera beat, after the spawn points have claimed their occupants")]
    [Min(0f)]
    [SerializeField] private float _preSequenceDelay = 0.25f;

    [Tooltip("Camera hold on the spawn point BEFORE the enemy emerges")]
    [Min(0f)]
    [SerializeField] private float _perEnemyPreHold = 0.3f;

    [Tooltip("Camera hold on the spawn point AFTER the enemy has emerged")]
    [Min(0f)]
    [SerializeField] private float _perEnemyPostHold = 0.4f;

    [Tooltip("Camera hold on the docking point AFTER the ship has come to a stop")]
    [Min(0f)]
    [SerializeField] private float _shipArrivalHold = 0.6f;

    [Tooltip("Wait after docking, before unblocking the turn loop and showing the HUD")]
    [Min(0f)]
    [SerializeField] private float _postDockDelay = 0.2f;

    [Header("Text")]
    [SerializeField] private string _openingLine = "The battle begins!";

    [Header("Camera")]
    [SerializeField] private CameraCueProfileSO _enemyFocusProfile;
    [SerializeField] private CameraCueProfileSO _shipFocusProfile;

    [Header("Audio")]
    [Tooltip("Music for the presentation cinematic: it plays while the camera sweeps over the enemies " +
        "and the ship docks, and fades out when the fight starts. It must have _loop = true and " +
        "_is3D = false (a non-positional 2D loop on the Music mixer) — the loop is what makes it " +
        "stoppable with a fade-out, not a way to repeat it.")]
    [SerializeField] private SoundEventSO _introMusic;

    [Tooltip("Fade-in duration of the intro music, in seconds")]
    [Min(0f)]
    [SerializeField] private float _introMusicFadeIn = 0.5f;

    [Tooltip("Fade-out duration of the intro music when the fight starts, in seconds")]
    [Min(0f)]
    [SerializeField] private float _introMusicFadeOut = 1f;

    [Tooltip("The combat's main soundtrack: it starts together with the \"The hunt is open!\" banner " +
        "and loops for the whole fight. It must have _loop = true and _is3D = false " +
        "(a non-positional 2D loop on the Music mixer).")]
    [SerializeField] private SoundEventSO _combatMusic;

    [Tooltip("Fade-in duration of the combat music, on the banner's beat")]
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
