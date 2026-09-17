using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// The authoring unit of a sound: clips, mix, spatialization, anti-spam.
/// Stateless by contract — the memory of "what was played last" lives on the AudioDirector and not here,
/// so a single asset can be shared by several callers.
/// </summary>
[CreateAssetMenu(fileName = "SoundEvent", menuName = "Audio/Sound Event")]
public class SoundEventSO : ScriptableObject
{
    [Header("Clips")]
    [Tooltip("With more than one, a random clip is picked on every playback")]
    [SerializeField] private AudioClip[] _clips;

    [Header("Mix")]
    [SerializeField] private AudioMixerGroup _mixerGroup;
    [Tooltip("Volume drawn at random from this range")]
    [SerializeField] private Vector2 _volumeRange = new(1f, 1f);
    [Tooltip("Pitch drawn at random from this range")]
    [SerializeField] private Vector2 _pitchRange = new(0.95f, 1.05f);

    [Header("Spatial")]
    [Tooltip("True = a positional sound in the world, False = 2D (UI, music)")]
    [SerializeField] private bool _is3D = true;
    [SerializeField] private AudioRolloffMode _rolloffMode = AudioRolloffMode.Logarithmic;
    [SerializeField] private float _minDistance = 3f;
    [SerializeField] private float _maxDistance = 25f;

    [Header("Looping")]
    [Tooltip("Looping sounds have to be started from the loop channel and stopped through their handle")]
    [SerializeField] private bool _loop;

    [Header("Anti-spam")]
    [Tooltip("Minimum seconds between two playbacks of THIS sound, whoever the caller is")]
    [Min(0f)] [SerializeField] private float _cooldownSeconds;
    [Tooltip("Maximum simultaneous voices of THIS sound across the whole scene")]
    [Min(1)] [SerializeField] private int _maxConcurrentInstances = 4;

    [Header("Pool")]
    [Tooltip("Lower = stolen first when the pool is exhausted")]
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
    /// Picks a clip. Pure: no state is kept. The caller passes the clip played last in
    /// <paramref name="avoid"/> to avoid immediate repeats.
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
