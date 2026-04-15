using UnityEngine;

public class CombatStateManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private TurnController _turnController;
    [SerializeField] private SelectionContextSO _selectionCtx;
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private TurnStateSO _currentTurnStateData;

    [Header("State Settings")]
    [SerializeField] private CombatStateSO _initialState;
    private CombatStateSO _activeState;

    private CombatContext _combatContext = new CombatContext();
    private int _lastTransitionFrame;
    public TurnController TurnController => _turnController;
    public SelectionContextSO SelectionCtx => _selectionCtx;
    public CombatContext CombatCtx => _combatContext;
    public TurnStateSO CurrentTurnStateData => _currentTurnStateData;

    void OnEnable()
    {
        _inputReader.RightClickEvent += OnRightClick;
    }

    void OnDisable()
    {
        _inputReader.RightClickEvent -= OnRightClick;
    }

    private void Start() => TransitionToState(_initialState);

    public void TransitionToState(CombatStateSO newStateAsset)
    {
        _activeState?.OnExit();
        
        _activeState = Instantiate(newStateAsset);
        _activeState.Init(this);
        
        _lastTransitionFrame = Time.frameCount;

        _activeState.OnEnter();
    }

    void Update()
    {
        _activeState?.OnUpdate();
    }

    public void OnPointerMoved(TargetingData data)  => _activeState?.HandlePointerMove(data);
    public void OnElementClicked(IInteractableElement element)
    {
        if (!CanProcessInput()) return;
        _activeState?.HandleElementClick(element);
    } 

    public void OnSelectAbility(IInteractableElement element)
    {
        if (!CanProcessInput()) return;
        _activeState?.HandleSelectAbility(element);
    } 

    public void OnRightClick()
    {
        if (!CanProcessInput()) return;
        _activeState?.HandleRightClick();
    }

    public void HandleGlobalClick(TargetingData data)
    {
        if (!CanProcessInput()) return;
        _activeState?.HandleGlobalClick(data);
    }

    private bool CanProcessInput()
    {
        return Time.frameCount > _lastTransitionFrame;
    }

    public void ClearCtxs()
    {
        ClearCtxs(null);
    }
    public void ClearCtxs(CombatStateSO toState)
    {
        SelectionCtx.ClearCtx();
        CombatCtx.ClearCtx();
        if (toState) TransitionToState(toState);
    }
}