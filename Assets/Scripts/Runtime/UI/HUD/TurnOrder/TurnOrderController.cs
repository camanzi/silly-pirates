using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TurnOrderController : MonoBehaviour
{
    [Header("Data Source")]
    [SerializeField] private TurnOrderDataSO _turnOrderData;
    [SerializeField] private TurnStateSO _currentTurnState;

    [Header("UI Templates")]
    [SerializeField] private VisualTreeAsset _turnCardTemplate;

    private UIDocument _uiDocument;
    private VisualElement root;
    private ListView turnListView;

    private readonly List<(EntityTurnState state, bool isSubTurn)> _displayList = new();

    void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        root = _uiDocument.rootVisualElement;

        turnListView = root.Q<ListView>("turn-list");

        if (turnListView == null)
        {
            Debug.LogError("ListView 'turn-list' non trovato nel UXML!");
            return;
        }

        SetupListView();
    }

    private void SetupListView()
    {
        RebuildDisplayList();

        turnListView.makeItem = () =>
        {
            TurnCard card = new TurnCard();
            _turnCardTemplate.CloneTree(card);
            card.InitElements();
            return card;
        };

        turnListView.bindItem = (element, index) => {
            if (element is TurnCard card && index < _displayList.Count)
            {
                var (state, isSubTurn) = _displayList[index];

                card.Data = new CharacterTurnData(
                    state.Agent,
                    state.CurrentAV,
                    state.Agent.RenderingData.TurnAgentIcon,
                    isSubTurn
                );

                card.IsActive = index == 0;
            }
        };

        turnListView.itemsSource = _displayList;

        turnListView.unbindItem = (element, index) => {};

        turnListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
        turnListView.selectionType = SelectionType.None;
    }

    private void RebuildDisplayList()
    {
        _displayList.Clear();
        bool isFirstAgent = true;
        foreach (var state in _turnOrderData.TurnQueue)
        {
            _displayList.Add((state, false));
            int total = state.Agent.AgentData.ActionsPerTurn - 1;
            int consumed = isFirstAgent ? _currentTurnState.CurrentActionIndex : 0;
            int remaining = Mathf.Max(0, total - consumed);
            for (int i = 0; i < remaining; i++)
                _displayList.Add((state, true));
            isFirstAgent = false;
        }
    }

    public void RefreshList()
    {
        if (turnListView == null) return;
        RebuildDisplayList();
        turnListView.itemsSource = _displayList;
        turnListView.RefreshItems();
    }
}
