using System.Collections.Generic;

/// <summary>
/// Handle dei VFX persistenti aperti durante una fase di lifecycle, con il canale su cui vanno spenti.
///
/// Vive nel <see cref="CharacterLifecycleAnimator"/> e NON nella <see cref="LifecycleAnimationSO"/>: quella
/// è un asset condiviso fra prefab diversi, quindi un campo lì verrebbe sovrascritto se due personaggi
/// eseguissero la stessa fase nello stesso frame. L'animator invece è per-istanza, ed è anche l'unico che
/// sa quando la fase finisce davvero — inclusa la fase preparata da <see cref="CharacterLifecycleAnimator.PrepareHidden"/>
/// e mai giocata, che nessun <c>finally</c> dentro la SO potrebbe intercettare.
///
/// Una sola istanza riusata fase dopo fase: nessuna allocazione per spawn ripetuto.
/// </summary>
public sealed class LifecycleVfxSession
{
    private readonly List<VfxHandle> _open = new();
    private VfxStopEventChannel _stopChannel;

    /// <summary>Una fase ha già alzato i suoi VFX di <see cref="LifecycleVfxStage.Prepare"/>.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>
    /// Apre la sessione per una nuova fase. Difensivo: chiude quella precedente se fosse rimasta aperta,
    /// così un loop non può mai sopravvivere alla fase che l'ha acceso.
    /// </summary>
    public void Begin(VfxStopEventChannel stopChannel)
    {
        StopAll();

        _stopChannel = stopChannel;
        IsOpen = true;
    }

    /// <summary>Registra un handle da spegnere a fine fase. Gli handle dei one-shot sono invalidi e si ignorano.</summary>
    public void Track(VfxHandle handle)
    {
        if (!handle.IsValid) return;
        _open.Add(handle);
    }

    /// <summary>
    /// Spegne tutti i persistenti aperti e chiude la sessione. Idempotente: un secondo stop sullo stesso
    /// handle è un miss no-op lato <see cref="VfxDirector"/>.
    /// </summary>
    public void StopAll()
    {
        if (_stopChannel != null)
        {
            for (int i = 0; i < _open.Count; i++)
                _stopChannel.RaiseEvent(_open[i]);
        }

        _open.Clear();
        _stopChannel = null;
        IsOpen = false;
    }
}
