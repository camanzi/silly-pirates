using UnityEngine;

public abstract class CombatStateSO : ScriptableObject
{
    [Header("Combat State configs")]
    [SerializeField] private bool _shouldShowUI;
    protected CombatStateManager manager;
    public bool ShouldShowUI => _shouldShowUI;
    public virtual void Init(CombatStateManager manager) => this.manager = manager;
    public virtual void OnEnter()
    {
        manager.ShowUIEventChannel.RaiseEvent(ShouldShowUI);
    }
    public abstract void OnExit();
    public abstract void OnUpdate();
    public abstract void HandleRightClick();
    public abstract void HandleElementClick(IInteractableElement element);
    public abstract void HandleSelectAbility(IInteractableElement element);
    public abstract void HandlePointerMove(TargetingData data);
    public abstract void HandleGlobalClick(TargetingData data);
}