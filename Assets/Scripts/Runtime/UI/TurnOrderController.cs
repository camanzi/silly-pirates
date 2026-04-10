using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class TurnOrderController : MonoBehaviour
{   
    [Header("Data Source")]
    [SerializeField] private TurnOrderDataSO _turnOrderData;

    [Header("UI Templates")]
    [SerializeField] private VisualTreeAsset _turnCardTemplate;
    
    private UIDocument _uiDocument;
    private VisualElement root;
    private ListView turnListView;

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
        turnListView.makeItem = () =>
        {
            TurnCard card = new TurnCard();
            _turnCardTemplate.CloneTree(card);
            card.InitElements();
            return card;
        };
        
        turnListView.bindItem = (element, index) => {
            if (element is TurnCard card && index < _turnOrderData.TurnQueue.Count)
            {
                var state = _turnOrderData.TurnQueue[index];
                
                card.Data = new CharacterTurnData(
                    state.Agent,
                    state.CurrentAV,
                    state.Agent.RenderingData.TurnAgentIcon
                );
                
                card.IsActive = index == 0;
            }
        };

        turnListView.itemsSource = _turnOrderData.TurnQueue;
        
        // Chiamato quando un elemento sta per essere riciclato
        turnListView.unbindItem = (element, index) => {};
        
        turnListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
        turnListView.selectionType = SelectionType.None;
    }

    public void RefreshList()
    {
        if (turnListView != null)
            turnListView.RefreshItems();
    }
}