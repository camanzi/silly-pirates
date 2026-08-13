using System;
using UnityEngine;

/// <summary>Dove il VFX di fase viene ancorato nel mondo.</summary>
public enum LifecycleVfxAnchor
{
    /// <summary>Transform di griglia del personaggio: fermo durante l'animazione, è il pelo dell'acqua.</summary>
    CharacterRoot,

    /// <summary>Pivot visivo del corpo, campionato una volta al momento del Raise (effetto fisso).</summary>
    BodyPivot,

    /// <summary>Insegue il pivot visivo del corpo per tutta la durata dell'effetto.</summary>
    FollowBody
}

/// <summary>
/// Momento interno di una <see cref="LifecycleAnimationSO"/> a cui è agganciata una lista di VFX.
/// Usato come chiave di dizionario, quindi i valori esistenti non vanno mai rinumerati
/// (stesso vincolo di <see cref="LifecyclePhase"/>).
/// </summary>
public enum LifecycleVfxStage
{
    /// <summary>
    /// Subito dopo che lo stato iniziale della fase è stato applicato (es. sprite già sott'acqua), prima
    /// che il movimento cominci. È l'unico stage raggiunto anche da <see cref="CharacterLifecycleAnimator.PrepareHidden"/>:
    /// durante l'intro al combattimento parte qui e resta acceso finché il regista non conclude la fase.
    /// </summary>
    Prepare,

    /// <summary>In sincro con l'avvio delle tween.</summary>
    Movement,

    /// <summary>A movimento concluso.</summary>
    End
}

/// <summary>
/// Descrittore inline di un VFX agganciato a uno stage di <see cref="LifecycleAnimationSO"/>.
/// <c>[Serializable]</c> e non uno ScriptableObject a parte: la configurazione va editata nello stesso
/// Inspector dell'animazione a cui appartiene, senza un asset satellite da creare e collegare.
/// </summary>
[Serializable]
public class LifecycleVfxSpec
{
    [SerializeField] private VFXController _prefab;
    [SerializeField] private LifecycleVfxAnchor _anchor = LifecycleVfxAnchor.CharacterRoot;

    [Tooltip("Effetto persistente: resta acceso finché la fase non finisce, invece di esaurirsi da solo.")]
    [SerializeField] private bool _loop;

    [Tooltip("Si applica solo agli ancoraggi fissi (CharacterRoot/BodyPivot): con FollowBody il VfxDirector " +
             "sovrascrive la posizione ad ogni LateUpdate (VfxDirector.cs), quindi l'offset verrebbe perso.")]
    [SerializeField] private Vector3 _offset;

    [SerializeField] private float _scale = 1f;

    public bool IsConfigured => _prefab != null;

    public bool Loop => _loop;

    /// <summary>
    /// Risolve l'ancoraggio, alza la cue giusta sul canale e restituisce l'handle coniato (solo per gli
    /// spec in <see cref="Loop"/>; <see cref="VfxHandle.None"/> per i one-shot).
    /// No-op difensivo se il canale manca o lo spec non è configurato.
    /// </summary>
    public VfxHandle Raise(VfxCueEventChannel channel, in LifecycleAnimationContext ctx)
    {
        if (channel == null || !IsConfigured) return VfxHandle.None;

        VfxHandle handle = _loop ? VfxHandle.New() : VfxHandle.None;
        channel.RaiseEvent(BuildCue(ctx, handle));
        return handle;
    }

    private VfxCue BuildCue(in LifecycleAnimationContext ctx, VfxHandle handle)
    {
        // Fallback difensivo: un satellite/corpo senza pivot valido non deve far crashare il Raise, si
        // ancora al transform di griglia come se fosse CharacterRoot.
        Transform bodyPivot = ctx.Body.Pivot != null ? ctx.Body.Pivot : ctx.Root;

        switch (_anchor)
        {
            case LifecycleVfxAnchor.FollowBody:
                return handle.IsValid
                    ? VfxCue.Persistent(_prefab, bodyPivot, handle, _scale)
                    : VfxCue.Follow(_prefab, bodyPivot, _scale);

            case LifecycleVfxAnchor.BodyPivot:
                return handle.IsValid
                    ? VfxCue.PersistentAt(_prefab, bodyPivot.position + _offset, handle, _scale)
                    : VfxCue.At(_prefab, bodyPivot.position + _offset, _scale);

            case LifecycleVfxAnchor.CharacterRoot:
            default:
                Vector3 position = ctx.Root.position + _offset;
                return handle.IsValid
                    ? VfxCue.PersistentAt(_prefab, position, handle, _scale)
                    : VfxCue.At(_prefab, position, _scale);
        }
    }
}
