using UnityEngine;

public class CombatStateManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private TurnController _turnController;
    [SerializeField] private SelectionContextSO _selectionCtx;

    [Header("State Settings")]
    [SerializeField] private CombatStateSO _initialState;
    private CombatStateSO _activeState;

    public TurnController turnController => _turnController;

    private CombatContext _combatContext = new CombatContext();

    public SelectionContextSO selectionCtx => _selectionCtx;
    public CombatContext combatCtx => _combatContext;

    private void Start() => TransitionToState(_initialState);

    public void TransitionToState(CombatStateSO newStateAsset)
    {
        _activeState?.OnExit();
        
        _activeState = Instantiate(newStateAsset);
        _activeState.Init(this);
        
        _activeState.OnEnter();
    }

    void Update()
    {
        _activeState?.OnUpdate();
    }

    public void OnElementClicked(GridElement element)  => _activeState?.HandleElementClick(element);

    public void OnPointerMoved(TargetingData data)  => _activeState?.HandlePointerMove(data);
}