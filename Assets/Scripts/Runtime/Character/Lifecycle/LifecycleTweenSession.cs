using System.Collections.Generic;
using PrimeTween;

/// <summary>
/// Handle dei tween PrimeTween aperti dal binario loop di lifecycle (vedi <see cref="LoopingLifecycleAnimationSO"/>).
/// Gemello di <see cref="LifecycleVfxSession"/> ma per i tween invece che per i VFX, e con la stessa
/// motivazione: vive nel <see cref="CharacterLifecycleAnimator"/> e NON nella SO, che resta condivisa fra
/// prefab diversi e quindi non può possedere handle per-istanza — due nemici con lo stesso asset di idle
/// si contenderebbero un unico campo.
///
/// È plurale perché un bersaglio composito (corpo + satelliti) fa partire N tween che vanno fermati
/// insieme; il corpo di un nemico standard è semplicemente il caso N=1.
///
/// Una sola istanza riusata loop dopo loop: nessuna allocazione per accensione ripetuta.
/// </summary>
public sealed class LifecycleTweenSession
{
    private readonly List<Tween> _open = new();

    /// <summary>Il loop è attualmente registrato come acceso. Specchio di <see cref="LifecycleVfxSession.IsOpen"/>.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>
    /// Apre la sessione per un nuovo loop. Difensivo come <see cref="LifecycleVfxSession.Begin"/>: ferma
    /// quella precedente, così un loop non può mai sopravvivere a quello che l'ha rimpiazzato.
    /// </summary>
    public void Begin()
    {
        StopAll();
        IsOpen = true;
    }

    /// <summary>Registra un tween da fermare con <see cref="StopAll"/>. I tween già morti (durata zero, target distrutto) si ignorano.</summary>
    public void Track(Tween tween)
    {
        if (tween.isAlive) _open.Add(tween);
    }

    /// <summary>
    /// Ferma tutti i tween aperti e chiude la sessione. Idempotente: uno <c>Stop()</c> su un tween già
    /// concluso è un no-op lato PrimeTween.
    /// </summary>
    public void StopAll()
    {
        for (int i = 0; i < _open.Count; i++)
            if (_open[i].isAlive) _open[i].Stop();

        _open.Clear();
        IsOpen = false;
    }
}
