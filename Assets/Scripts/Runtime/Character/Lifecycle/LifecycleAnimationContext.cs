using UnityEngine;

/// <summary>
/// Contesto passato alle <see cref="LifecycleAnimationSO"/> perché restino stateless:
/// tutto lo stato mutabile vive qui, non nella SO.
/// </summary>
public readonly struct LifecycleAnimationContext
{
    public readonly Transform Root;                // transform del personaggio (posizione di griglia) — NON toccare
    public readonly Transform AnimationRoot;        // pivot visivo da animare
    public readonly SpriteRenderer Renderer;        // per fade/color
    public readonly Vector3 RestLocalPosition;      // pose a riposo catturata in Awake
    public readonly Vector3 RestLocalScale;
    public readonly Color RestColor;

    public LifecycleAnimationContext(
        Transform root,
        Transform animationRoot,
        SpriteRenderer renderer,
        Vector3 restLocalPosition,
        Vector3 restLocalScale,
        Color restColor)
    {
        Root = root;
        AnimationRoot = animationRoot;
        Renderer = renderer;
        RestLocalPosition = restLocalPosition;
        RestLocalScale = restLocalScale;
        RestColor = restColor;
    }
}
