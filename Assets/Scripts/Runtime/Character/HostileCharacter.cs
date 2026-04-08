using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(OutlinerHelper))]
public class HostileCharacter : MonoBehaviour, ISelectable, IInteractableElement, ITargettable, ITurnAgent
{
    [Header("Hostile Character configuration")]
    [SerializeField] private TurnAgentDataSO _agentData;

    [Header("External references")]
    [SerializeField] private SelectionContextSO _selectionContextSO;
    
    [Header("Event channels")]
    [SerializeField] private InteractableElementEventChannel _elementClickedChannel;

    [Header("Combat System event channels")]
    [SerializeField] private TurnAgentEventChannel _onAgentJoin;
    [SerializeField] private TurnAgentEventChannel _onAgentLeave;

    public Transform Transform => transform;

    public OutlinerHelper OutlinerHelper => _outlinerHelper;

    public SelectionContextSO SelectionContext => _selectionContextSO;

    public InteractableElementEventChannel ClickChannel => _elementClickedChannel;

    TurnAgentEventChannel ITurnAgent.OnAgentJoin => _onAgentJoin;

    TurnAgentEventChannel ITurnAgent.OnAgentLeave => _onAgentLeave;
    public TurnAgentDataSO AgentData => _agentData;

    private OutlinerHelper _outlinerHelper;

    void Awake()
    {
        _outlinerHelper = GetComponent<OutlinerHelper>();
    }

    protected virtual void OnEnable()
    {
        OnCombatJoin();
    }

    public void OnPointerEnter(PointerEventData eventData) => this.HandlePointerEnter();

    public void OnPointerClick(PointerEventData eventData) => this.HandlePointerClick();

    public void OnPointerExit(PointerEventData eventData) => this.HandlePointerExit();

    public void OnSelectionCtxChange() => this.HandlePointerExit();

    public void OnCombatJoin() => this.HandleCombatJoin();

    public void OnCombatLeave() => this.HandleCombatLeave();

    public void OnStartingTurn() => this.HandleStartingTurn();
}
