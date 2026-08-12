using UnityEngine;

/// <summary>
/// Richiesta di riprodurre un effetto particellare. Fire-and-forget come <see cref="SfxCue"/>:
/// nessuno aspetta che finisca, e il VfxDirector rimette l'istanza nel pool da solo.
///
/// L'unica eccezione e' il VFX persistente, che porta un <see cref="VfxHandle"/> valido e va
/// fermato esplicitamente sul canale di stop.
/// </summary>
public struct VfxCue
{
    public VFXController Prefab;

    /// <summary>Posizione nel mondo. Ignorata se FollowTarget e' valorizzato.</summary>
    public Vector3 Position;

    public Quaternion Rotation;

    /// <summary>Scala uniforme applicata all'istanza. 0 viene letto come 1.</summary>
    public float Scale;

    /// <summary>Se valorizzato, l'istanza insegue questo Transform invece di stare ferma.</summary>
    public Transform FollowTarget;

    /// <summary>Valido = effetto persistente: nessun rilascio automatico, si ferma con lo stop channel.</summary>
    public VfxHandle Handle;

    /// <summary>Effetto one-shot in un punto fisso: nessun costo per frame.</summary>
    public static VfxCue At(VFXController prefab, Vector3 position, float scale = 1f) => new()
    {
        Prefab = prefab,
        Position = position,
        Rotation = Quaternion.identity,
        Scale = scale,
        FollowTarget = null,
        Handle = VfxHandle.None
    };

    /// <summary>Effetto one-shot orientato, per muzzle flash e impatti direzionali.</summary>
    public static VfxCue At(VFXController prefab, Vector3 position, Quaternion rotation, float scale = 1f) => new()
    {
        Prefab = prefab,
        Position = position,
        Rotation = rotation,
        Scale = scale,
        FollowTarget = null,
        Handle = VfxHandle.None
    };

    /// <summary>Effetto one-shot che insegue un bersaglio in movimento.</summary>
    public static VfxCue Follow(VFXController prefab, Transform target, float scale = 1f) => new()
    {
        Prefab = prefab,
        Position = target != null ? target.position : Vector3.zero,
        Rotation = Quaternion.identity,
        Scale = scale,
        FollowTarget = target,
        Handle = VfxHandle.None
    };

    /// <summary>
    /// Effetto che resta acceso finche' non viene fermato con l'handle. Insegue il bersaglio
    /// invece di essergli parentato: un oggetto del pool non va mai riparentato su un oggetto di gameplay.
    /// </summary>
    public static VfxCue Persistent(VFXController prefab, Transform target, VfxHandle handle, float scale = 1f) => new()
    {
        Prefab = prefab,
        Position = target != null ? target.position : Vector3.zero,
        Rotation = Quaternion.identity,
        Scale = scale,
        FollowTarget = target,
        Handle = handle
    };
}
