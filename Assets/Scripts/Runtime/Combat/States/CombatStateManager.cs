using System.Collections.Generic;
using UnityEngine;

public class CombatStateManager : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private CommandQueueSO _commandQueue;
    [SerializeField] private AbilityRendererSO _abilityRenderer;
    [SerializeField] private AbilityCostEventChannel _abilityCostChannel;
    [SerializeField] private SelectionContextSO _selectionCtx;
    [SerializeField] private InputReader _inputReader;
    [SerializeField] private TurnStateSO _currentTurnStateData;
    [SerializeField] private BoolEventChannel _showUIEventChannel;
    [SerializeField] private CameraDirectorStateSO _cameraDirectorState;
    [SerializeField] private AbilityExecutionCueEventChannel _cameraCueChannel;
    [SerializeField] private SfxCueEventChannel _sfxChannel;
    [Tooltip("Announces to the targets that an ability is about to hit them, and when the execution is over")]
    [SerializeField] private AbilityThreatEventChannel _threatChannel;
    [Tooltip("Optional: while paused no input reaches the state machine. Leave empty in scenes with no pause menu")]
    [SerializeField] private PauseStateSO _pauseState;

    [Header("State Settings")]
    [SerializeField] private CombatStateSO _initialState;
    private CombatStateSO _activeState;

    private CombatContext _combatContext = new CombatContext();
    private int _lastTransitionFrame;
    public CommandQueueSO CommandQueue => _commandQueue;
    public AbilityRendererSO AabilityRenderer => _abilityRenderer;
    public AbilityCostEventChannel AbilityCostChannel => _abilityCostChannel;
    public SelectionContextSO SelectionCtx => _selectionCtx;
    public CombatContext CombatCtx => _combatContext;
    public TurnStateSO CurrentTurnStateData => _currentTurnStateData;
    public BoolEventChannel ShowUIEventChannel => _showUIEventChannel;
    public CameraDirectorStateSO CameraDirectorState => _cameraDirectorState;
    public AbilityExecutionCueEventChannel CameraCueChannel => _cameraCueChannel;
    public SfxCueEventChannel SfxChannel => _sfxChannel;
    public AbilityThreatEventChannel ThreatChannel => _threatChannel;

    private Dictionary<CombatStateSO, CombatStateSO> _stateInstances = new Dictionary<CombatStateSO, CombatStateSO>();

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
        if (newStateAsset == null) return;

        _activeState?.OnExit();
        
        if (!_stateInstances.ContainsKey(newStateAsset))
        {
            CombatStateSO stateClone = Instantiate(newStateAsset);
            stateClone.Init(this);
            _stateInstances[newStateAsset] = stateClone;
        }
        
        _activeState = _stateInstances[newStateAsset];
        
        _lastTransitionFrame = Time.frameCount;
        _activeState.OnEnter();
    }

    void Update()
    {
        _activeState?.OnUpdate();
    }

    void OnDestroy()
    {
        foreach (var stateInstance in _stateInstances.Values)
        {
            if (stateInstance != null) Destroy(stateInstance);
        }
        _stateInstances.Clear();
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

    public void RequestActiveAbility(ActiveAbilityRequestData data)
    {
        if (!CanProcessInput()) return;
        _activeState?.HandleActiveAbilityRequest(data);
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
        // Clicks on the world are already swallowed by the pause menu's full-screen pickable root
        // (WorldInteractor and GridInputHandler consult UIPointerTracker.IsPointerOverUI first). This
        // guard makes the intent explicit and covers the paths that are not pointer-driven.
        if (_pauseState != null && _pauseState.IsPaused) return false;

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