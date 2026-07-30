using System.Threading;
using UnityEngine;

/// <summary>
/// Animazione di lifecycle per un personaggio (spawn, morte, ecc.), mirror di <see cref="HealthBehaviorSO"/>.
/// Le implementazioni devono restare stateless: tutto lo stato mutabile vive in <see cref="LifecycleAnimationContext"/>,
/// così un solo asset condiviso può essere referenziato da più prefab senza Instantiate per-istanza.
/// </summary>
public abstract class LifecycleAnimationSO : ScriptableObject
{
    /// <summary>
    /// Applica istantaneamente lo stato iniziale (es. sprite già sott'acqua).
    /// Chiamata nello stesso frame in cui il GO viene attivato, prima del rendering.
    /// </summary>
    public virtual void Prepare(in LifecycleAnimationContext ctx) { }

    public abstract Awaitable PlayAsync(LifecycleAnimationContext ctx, CancellationToken token);
}
