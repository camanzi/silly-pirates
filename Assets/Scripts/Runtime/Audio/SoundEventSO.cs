using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// L'unita' di autoring di un suono: clip, mix, spazializzazione, anti-spam.
/// Stateless per contratto — la memoria di "cosa e' stato suonato per ultimo" vive
/// sull'AudioDirector, non qui, cosi' un solo asset puo' essere condiviso da piu' chiamanti.
/// </summary>
[CreateAssetMenu(fileName = "SoundEvent", menuName = "Audio/Sound Event")]
public class SoundEventSO : ScriptableObject
{
    [Header("Clips")]
    [Tooltip("Se piu' di una, ne viene scelta una a caso a ogni riproduzione")]
    [SerializeField] private AudioClip[] _clips;

    [Header("Mix")]
    [SerializeField] private AudioMixerGroup _mixerGroup;
    [Tooltip("Volume estratto a caso in questo intervallo")]
    [SerializeField] private Vector2 _volumeRange = new(1f, 1f);
    [Tooltip("Pitch estratto a caso in questo intervallo")]
    [SerializeField] private Vector2 _pitchRange = new(0.95f, 1.05f);

    [Header("Spatial")]
    [Tooltip("True = suono posizionale nel mondo, False = 2D (UI, musica)")]
    [SerializeField] private bool _is3D = true;
    [SerializeField] private AudioRolloffMode _rolloffMode = AudioRolloffMode.Logarithmic;
    [SerializeField] private float _minDistance = 3f;
    [SerializeField] private float _maxDistance = 25f;

    [Header("Looping")]
    [Tooltip("I suoni in loop vanno avviati dal canale di loop e fermati con il loro handle")]
    [SerializeField] private bool _loop;

    [Header("Anti-spam")]
    [Tooltip("Secondi minimi fra due riproduzioni di QUESTO suono, chiunque sia il chiamante")]
    [Min(0f)] [SerializeField] private float _cooldownSeconds;
    [Tooltip("Voci simultanee massime di QUESTO suono nell'intera scena")]
    [Min(1)] [SerializeField] private int _maxConcurrentInstances = 4;

    [Header("Pool")]
    [Tooltip("Piu' basso = viene rubato per primo quando il pool e' esaurito")]
    [Range(0, 10)] [SerializeField] private int _stealPriority = 5;

    public AudioMixerGroup MixerGroup => _mixerGroup;
    public Vector2 VolumeRange => _volumeRange;
    public Vector2 PitchRange => _pitchRange;
    public bool Is3D => _is3D;
    public AudioRolloffMode RolloffMode => _rolloffMode;
    public float MinDistance => _minDistance;
    public float MaxDistance => _maxDistance;
    public bool Loop => _loop;
    public float CooldownSeconds => _cooldownSeconds;
    public int MaxConcurrentInstances => _maxConcurrentInstances;
    public int StealPriority => _stealPriority;
    public bool HasClips => _clips != null && _clips.Length > 0;

    /// <summary>
    /// Sceglie una clip. Puro: nessuno stato conservato. Il chiamante passa l'ultima clip
    /// suonata in <paramref name="avoid"/> per evitare ripetizioni immediate.
    /// </summary>
    public AudioClip PickClip(AudioClip avoid = null)
    {
        if (_clips == null || _clips.Length == 0) return null;
        if (_clips.Length == 1) return _clips[0];

        AudioClip picked = _clips[Random.Range(0, _clips.Length)];

        if (picked == avoid)
        {
            int next = (System.Array.IndexOf(_clips, picked) + 1) % _clips.Length;
            picked = _clips[next];
        }

        return picked;
    }

    public float PickVolume() => Random.Range(_volumeRange.x, _volumeRange.y);

    public float PickPitch() => Random.Range(_pitchRange.x, _pitchRange.y);
}
