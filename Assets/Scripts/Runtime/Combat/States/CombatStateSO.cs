using UnityEngine;

public abstract class CombatStateSO : ScriptableObject
{
    protected CombatStateManager manager;

    public virtual void Init(CombatStateManager manager) => this.manager = manager;

    public abstract void OnEnter();
    public abstract void OnExit();
    public abstract void OnUpdate();
    public abstract void HandleRightClick();
    public abstract void HandleElementClick(IInteractableElement element);
    public abstract void HandlePointerMove(TargetingData data);
    public abstract void HandleGlobalClick(TargetingData data);
}