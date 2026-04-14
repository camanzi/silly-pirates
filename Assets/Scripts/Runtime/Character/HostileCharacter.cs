using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(OutlinerHelper))]
public class HostileCharacter : MonoBehaviour, ISelectable, IInteractableElement, ITargettable, ITurnAgent
{
    [Header("Turn Agent configurations")]
    [SerializeField] private TurnAgentDataSO _agentData;
    [SerializeField] private TurnRenderingAgentDataSO _renderingAgentData;

    [Header("External references")]
    [SerializeField] private SelectionContextSO _selectionContextSO;
    
    [Header("Event channels")]
    [SerializeField] private InteractableElementEventChannel _elementClickedChannel;

    [Header("Combat System event channels")]
    [SerializeField] private TurnAgentEventChannel _onAgentJoin;
    [SerializeField] private TurnAgentEventChannel _onAgentLeave;
    [SerializeField] private TurnStateSO _currentTurnStateData;

    [Header("Proximity Logic")]
    [SerializeField] protected InteractableProximityEventChannel _proximityChannel;

    public Transform Transform => transform;
    public OutlinerHelper OutlinerHelper => _outlinerHelper;
    public TurnRenderingAgentDataSO RenderingData => _renderingAgentData;
    public TurnAgentDataSO AgentData => _agentData;
    public SelectionContextSO SelectionContext => _selectionContextSO;
    public InteractableElementEventChannel ClickChannel => _elementClickedChannel;
    public TurnAgentEventChannel OnAgentJoin => _onAgentJoin;
    public TurnAgentEventChannel OnAgentLeave => _onAgentLeave;
    public InteractableProximityEventChannel ProximityChannel => _proximityChannel;
    private OutlinerHelper _outlinerHelper;

    void Awake()
    {
        _outlinerHelper = GetComponent<OutlinerHelper>();
    }

    protected virtual void OnEnable()
    {
        OnCombatJoin();
    }

     public void OnHoverEnter() => this.HandlePointerEnter();

    public void OnClick() => this.HandlePointerClick();

    public void OnHoverExit() => this.HandlePointerExit();

    public void OnSelectionCtxChange() => this.HandlePointerExit();

    public void OnCombatJoin() => this.HandleCombatJoin();

    public void OnCombatLeave() => this.HandleCombatLeave();

    public async void OnStartingTurn()
    {
        this.HandleStartingTurn();
        this.EmitProximityCheck(ProximityPayload.Empty);

        await Awaitable.WaitForSecondsAsync(2.5f);

        _currentTurnStateData.SignalTurnEnd();
    }
}
