using UnityEngine;

/// <summary>
/// Runtime-only state declaring that a UI element (currently a turn-order card) is standing in for a
/// world-space interactable: while <see cref="Current"/> is set, WorldInteractor and GridInputHandler
/// behave as if their raycast had hit that element.
///
/// Why a state ScriptableObject and not an event channel: the two consumers live on different prefabs and
/// must re-read the condition every frame. An event pulse would lose the race against
/// WorldInteractor.ResetHover() (Update) and against the TargetingData.Empty that GridInputHandler.LateUpdate
/// raises as soon as the pointer moves over the UI. A polled flag is re-asserted every frame instead, so the
/// order in which those resets happen stops mattering.
/// </summary>
[CreateAssetMenu(fileName = "Interaction Proxy", menuName = "Interactables/Interaction Proxy")]
public class InteractionProxySO : ScriptableObject, ICombatSessionResettable
{
    public IInteractableElement Current { get; private set; }

    public void SetProxy(IInteractableElement element) => Current = element;

    /// <summary>
    /// Clears the proxy only when <paramref name="element"/> is still the current one, so a late PointerLeave
    /// coming from an old card cannot cancel the PointerEnter of the new one.
    /// </summary>
    public void ClearProxy(IInteractableElement element)
    {
        if (element == Current) Current = null;
    }

    // Without this, Current stays as a pointer to an IInteractableElement destroyed by the previous
    // scene's unload.
    public void ResetForNewCombat() => Current = null;
}
