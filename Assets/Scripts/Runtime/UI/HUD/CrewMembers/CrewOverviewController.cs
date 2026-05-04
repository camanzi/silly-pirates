using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class CrewOverviewController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private UIDocument _uiDocument;
    [SerializeField] private VisualTreeAsset _crewMemberTemplate;

    [Header("Events & State")]
    [SerializeField] private TurnStateSO _turnState;

    private VisualElement _crewContainer;
    private List<CrewMemberIndicator> _activeIndicators = new List<CrewMemberIndicator>();

    private void OnEnable()
    {
        var root = _uiDocument.rootVisualElement;
        
        _crewContainer = root.Q<VisualElement>("crew-container");
        foreach (var indicator in _activeIndicators)
        {
            _crewContainer.Add(indicator);
        }

        _turnState.OnAgentActivated.OnEventRaised += HandleTurnChanged;
    }

    private void OnDisable()
    {
        _turnState.OnAgentActivated.OnEventRaised -= HandleTurnChanged;
    }

    public void HandleAgentJoined(ITurnAgent agent)
    {
        if (!agent.CompareTag("Player")) return;
        
        var newIndicator = new CrewMemberIndicator();
        
        newIndicator.Initialize(agent, _crewMemberTemplate);

        if (_crewContainer != null)
            _crewContainer.Add(newIndicator);
        _activeIndicators.Add(newIndicator);
        
        newIndicator.UpdateTurnState(_turnState.ActiveAgent);    
    }

    private void HandleTurnChanged(ITurnAgent activeAgent)
    {
        foreach (var indicator in _activeIndicators)
        {
            indicator.UpdateTurnState(activeAgent);
        }
    }
}