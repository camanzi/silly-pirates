using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class EndTurnButton : MonoBehaviour
{   
    [Header("Data Source")]
    [SerializeField] private TurnStateSO _currentTurnStateData;
    
    private UIDocument _uiDocument;
    private VisualElement _root;
    private Button _endTurnButton;

    void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        _root = _uiDocument.rootVisualElement;
        
        _endTurnButton = _root.Q<Button>("end-turn-button");
        _endTurnButton.clicked += OnEndTurnClick;
    }

    private void OnDisable()
    {
        _endTurnButton.clicked -= OnEndTurnClick;       
    }

    public void UpdateEndTurnButtonState()
    {
        _endTurnButton.SetEnabled(_currentTurnStateData.IsPlayerTurn);
    }

    private void OnEndTurnClick()
    {
        _currentTurnStateData.SignalTurnEnd();
    }
}