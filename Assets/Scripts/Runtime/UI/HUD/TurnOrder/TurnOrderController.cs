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

    private const float SlideEpsilon = 0.5f;

    private UIDocument _uiDocument;
    private VisualElement _root;
    private VisualElement _turnCardRow;

    private readonly List<TurnGroupDisplay> _displayList = new();
    private readonly List<TurnCardStack> _activeStacks = new();
    private ITurnAgent _hoveredAgent;

    void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        _root = _uiDocument.rootVisualElement;

        _turnCardRow = _root.Q<VisualElement>("turn-card-row");

        if (_turnCardRow == null)
        {
            Debug.LogError("VisualElement 'turn-card-row' non trovato nel UXML!");
            return;
        }

        RefreshList();
    }

    private void OnDisable()
    {
        foreach (var stack in _activeStacks)
        {
            stack.Unbind();
            stack.RemoveFromHierarchy();
        }
        _activeStacks.Clear();
    }

    private void RebuildDisplayList()
    {
        _displayList.Clear();
        bool isFirstAgent = true;
        foreach (var state in _turnOrderData.TurnQueue)
        {
            int total = state.Agent.AgentData.ActionsPerTurn - 1;
            int consumed = isFirstAgent ? _currentTurnState.CurrentActionIndex : 0;
            int remaining = Mathf.Max(0, total - consumed);
            _displayList.Add(new TurnGroupDisplay(state, remaining));
            isFirstAgent = false;
        }
    }

    public void RefreshList()
    {
        if (_turnCardRow == null) return;
        RebuildDisplayList();
        ReconcileCards();
    }

    private void ReconcileCards()
    {
        // Snapshot current on-screen X of every live stack before we touch data/order,
        // so we can FLIP-animate the ones that end up moving.
        var oldX = new Dictionary<TurnCardStack, float>(_activeStacks.Count);
        foreach (var stack in _activeStacks)
            oldX[stack] = stack.layout.x;

        var pool = new Dictionary<ITurnAgent, TurnCardStack>(_activeStacks.Count);
        foreach (var stack in _activeStacks)
            pool[stack.Agent] = stack;

        var newActiveStacks = new List<TurnCardStack>(_displayList.Count);
        var retainedStacks = new List<TurnCardStack>();

        for (int i = 0; i < _displayList.Count; i++)
        {
            var group = _displayList[i];
            bool isActive = i == 0;

            TurnCardStack stack;
            if (pool.Remove(group.State.Agent, out stack))
                retainedStacks.Add(stack);
            else
                stack = new TurnCardStack();

            stack.Bind(group.State, group.SubTurnCount, isActive, _turnCardTemplate);
            stack.SetHovered(_hoveredAgent != null && group.State.Agent == _hoveredAgent);

            // VisualElement.Add() re-parents an already-attached element to the end of
            // its container, so calling it in display-list order rebuilds sibling order.
            _turnCardRow.Add(stack);
            newActiveStacks.Add(stack);
        }

        // Anything left unclaimed in the pool no longer exists in the new display list.
        foreach (var leftover in pool.Values)
        {
            leftover.Unbind();
            leftover.RemoveFromHierarchy();
        }

        _activeStacks.Clear();
        _activeStacks.AddRange(newActiveStacks);

        if (retainedStacks.Count == 0) return;

        // Layout (Yoga) is recalculated lazily on the next layout pass, so defer reading
        // the post-reorder X by a frame before starting the FLIP-style slide-in.
        _turnCardRow.schedule.Execute(() =>
        {
            foreach (var stack in retainedStacks)
            {
                if (!oldX.TryGetValue(stack, out float previousX)) continue;
                float delta = previousX - stack.layout.x;
                if (Mathf.Abs(delta) > SlideEpsilon)
                    stack.PlaySlideFrom(delta);
            }
        }).ExecuteLater(0);
    }

    public void OnElementHovered(IInteractableElement element)
    {
        _hoveredAgent = element as ITurnAgent;
        RefreshHoverStates();
    }

    private void RefreshHoverStates()
    {
        for (int i = 0; i < _activeStacks.Count && i < _displayList.Count; i++)
        {
            _activeStacks[i].SetHovered(_hoveredAgent != null && _displayList[i].State.Agent == _hoveredAgent);
        }
    }
}
