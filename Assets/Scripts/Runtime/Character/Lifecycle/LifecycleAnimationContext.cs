using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contesto passato alle <see cref="LifecycleAnimationSO"/> perché restino stateless:
/// tutto lo stato mutabile vive qui, non nella SO.
/// </summary>
public readonly struct LifecycleAnimationContext
{
    private static readonly LifecycleAnimationTarget[] NoTargets = new LifecycleAnimationTarget[0];

    public readonly Transform Root;   // transform del personaggio (posizione di griglia) — NON toccare

    private readonly IReadOnlyList<LifecycleAnimationTarget> _targets;

    /// <summary>
    /// Bersagli da animare: <c>[0]</c> è sempre il corpo, seguito dai satelliti registrati.
    /// La lista è VIVA — è la stessa istanza posseduta dal <see cref="CharacterLifecycleAnimator"/> — così
    /// un satellite registrato dopo la costruzione del contesto (l'ordine fra gli <c>Awake</c> non è
    /// garantito) compare qui senza dover ricostruire il contesto.
    /// </summary>
    public IReadOnlyList<LifecycleAnimationTarget> Targets => _targets ?? NoTargets;

    public LifecycleAnimationTarget Body => Targets.Count > 0 ? Targets[0] : default;

    // Scorciatoie sul corpo: mantengono invariata la sintassi delle SO e dei chiamanti scritti quando il
    // contesto aveva un solo bersaglio.
    public Transform AnimationRoot => Body.Pivot;
    public SpriteRenderer Renderer => Body.Renderer;
    public Vector3 RestLocalPosition => Body.RestLocalPosition;
    public Vector3 RestLocalScale => Body.RestLocalScale;
    public Color RestColor => Body.RestColor;

    public LifecycleAnimationContext(Transform root, IReadOnlyList<LifecycleAnimationTarget> targets)
    {
        Root = root;
        _targets = targets;
    }
}
