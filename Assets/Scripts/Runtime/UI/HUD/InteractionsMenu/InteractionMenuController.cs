using UnityEngine;
using UnityEngine.UIElements;
using PrimeTween;
using System.Collections.Generic;
using System.Linq;

public class InteractionMenuController : WorldSpaceContainer
{
    [Header("Menu settings")]
    [SerializeField] private InteractionSetSO _interactionSet;
    [SerializeField] private float _buttonRadius = 175f;

    [Header("Dependences")]
    [SerializeField] private InteractableGridElement _bindedMenuElement;
    [SerializeField] private TurnStateSO _currentTurnState;
    [SerializeField] private AbilityCostEventChannel _abilityHoverChannel;

    private Dictionary<InteractionActionSO, InteractionButton> _activeButtons = new();
    private VisualElement _statusContainer;
    private EquipmentStatusElement _statusElement;

    protected override void Awake()
    {
        base.Awake();
        _statusContainer = _uiDocument.rootVisualElement.Q<VisualElement>("status-container");
        _statusContainer.pickingMode = PickingMode.Position;
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        if (Container != null)
        {
            Container.style.opacity = 0f;
            Container.style.display = DisplayStyle.None;
        }

        if (_statusContainer != null)
        {
            _statusContainer.style.display = DisplayStyle.Flex;
            _statusElement = new EquipmentStatusElement();
            _statusContainer.Add(_statusElement);
            SetupStatusElement();
        }

        var awakable = _bindedMenuElement as IAwakable;
        if (awakable != null)
        {
            awakable.OnAwakeningCountersChanged += OnAwakeningStateChanged;
            awakable.OnCooldownChanged += OnCooldownStateChanged;
            awakable.OnAwakeningHoverPreview += OnHoverPreview;
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        if (_statusContainer != null && _statusElement != null)
        {
            _statusContainer.Remove(_statusElement);
            _statusElement = null;
        }

        var awakable = _bindedMenuElement as IAwakable;
        if (awakable != null)
        {
            awakable.OnAwakeningCountersChanged -= OnAwakeningStateChanged;
            awakable.OnCooldownChanged -= OnCooldownStateChanged;
            awakable.OnAwakeningHoverPreview -= OnHoverPreview;
        }
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();

        if (MainCamera == null || _statusContainer == null || _statusContainer.panel == null) return;
        Vector2 panelPos = RuntimePanelUtils.CameraTransformWorldToPanel(
            _statusContainer.panel, transform.position + _positionOffset, MainCamera);
        _statusContainer.style.left = panelPos.x;
        _statusContainer.style.top = panelPos.y;
    }

    protected override void ShowUI()
    {
        base.ShowUI();
        Container.style.display = DisplayStyle.Flex;
        BuildMenu();
    }

    public void BuildMenu()
    {
        Container.Clear();
        _activeButtons.Clear();
        float delay = 0f;

        foreach (InteractionActionSO action in _interactionSet.AvailableActions)
        {
            if (!ShouldShowAction(action)) continue;

            if (!_activeButtons.ContainsKey(action))
            {
                CreateInteractionButton(action, delay);
                delay += 0.1f;
            }
        }

        RepositionButtons();
        UpdateEnabledStates();
    }

    protected override void OnCompleteHide()
    {
        base.OnCompleteHide();
        _activeButtons.Clear();
        _abilityHoverChannel?.RaiseEvent(AbilityCostPayload.Empty);
        (_bindedMenuElement as IAwakable)?.OnAwakeningHoverPreview?.Invoke(0);
    }

    protected override void OnCompleteShow()
    {
        base.OnCompleteShow();
    }

    protected override void RefreshUI()
    {
        base.RefreshUI();
        List<InteractionActionSO> available = _interactionSet.AvailableActions;
        float delay = 0f;

        var toRemove = new List<InteractionActionSO>();
        foreach (KeyValuePair<InteractionActionSO, InteractionButton> pair in _activeButtons)
        {
            if (!available.Contains(pair.Key) || !ShouldShowAction(pair.Key))
                toRemove.Add(pair.Key);
        }

        var toAdd = new List<InteractionActionSO>();
        foreach (InteractionActionSO action in available)
        {
            if (ShouldShowAction(action) && !_activeButtons.ContainsKey(action) && !toAdd.Contains(action))
                toAdd.Add(action);
        }

        foreach (InteractionActionSO newAction in toAdd.ToList())
        {
            if (newAction is not IInPlaceSwappable swappable) continue;
            if (!toRemove.Contains(swappable.BaseAction)) continue;

            InteractionButton btn = _activeButtons[swappable.BaseAction];
            _activeButtons.Remove(swappable.BaseAction);
            _activeButtons[newAction] = btn;
            btn.UpdateData(newAction);

            toRemove.Remove(swappable.BaseAction);
            toAdd.Remove(newAction);
        }

        foreach (InteractionActionSO action in toRemove)
        {
            InteractionButton btn = _activeButtons[action];
            _activeButtons.Remove(action);

            Tween.Custom(1f, 0f, duration: 0.2f, onValueChange: v => {
                btn.style.scale = new StyleScale(new Scale(new Vector3(v, v, 1f)));
            }).OnComplete(() => {
                Container.Remove(btn);
                RepositionButtons();
            });
        }

        foreach (InteractionActionSO action in toAdd)
        {
            CreateInteractionButton(action, delay);
            delay += 0.1f;
        }

        RepositionButtons();
        UpdateEnabledStates();
    }

    private void CreateInteractionButton(InteractionActionSO action, float delay)
    {
        InteractionButton btn = new InteractionButton();
        btn.SetData(action, _bindedMenuElement, _currentTurnState.ActiveAgent, RefreshUI, _abilityHoverChannel);

        btn.style.position = Position.Absolute;
        btn.style.scale = new StyleScale(new Scale(Vector3.zero));
        Container.Add(btn);
        _activeButtons.Add(action, btn);

        Tween.Custom(0f, 1f, duration: 0.3f, startDelay: delay, ease: Ease.OutBack, onValueChange: v => {
            btn.style.scale = new StyleScale(new Scale(new Vector3(v, v, 1f)));
        });
    }

    private void RepositionButtons()
    {
        var buttons = _activeButtons.Values.ToList();
        int count = buttons.Count;
        if (count == 0) return;

        float angleStep = 360f / count;
        for (int i = 0; i < count; i++)
        {
            float rad = (-90f + i * angleStep) * Mathf.Deg2Rad;
            buttons[i].style.left = _buttonRadius * Mathf.Cos(rad);
            buttons[i].style.top = _buttonRadius * Mathf.Sin(rad);
        }
    }

    private void SetupStatusElement()
    {
        if (_statusElement == null || _interactionSet == null) return;
        _statusElement.SetData(
            _interactionSet.MainAction,
            _interactionSet.CooldownIcon,
            _bindedMenuElement,
            _currentTurnState?.ActiveAgent,
            _bindedMenuElement as IAwakable,
            OnStatusActionExecuted,
            _abilityHoverChannel);
    }

    private void OnStatusActionExecuted()
    {
        _statusElement?.RefreshAwakening();
        if (_isVisible) RefreshUI();
    }

    private void OnAwakeningStateChanged()
    {
        _statusElement?.UpdateAgent(_currentTurnState?.ActiveAgent);
        _statusElement?.RefreshAwakening();
        if (_isVisible) RefreshUI();
    }

    private void OnCooldownStateChanged(int cooldown)
    {
        _statusElement?.UpdateAgent(_currentTurnState?.ActiveAgent);
        _statusElement?.RefreshCooldown(cooldown);
        if (_isVisible) RefreshUI();
    }

    private void OnHoverPreview(int amount)
    {
        _statusElement?.SetHoverPreview(amount);
    }

    private bool ShouldShowAction(InteractionActionSO action)
        => action.CanShow(_bindedMenuElement, _currentTurnState.ActiveAgent);

    private void UpdateEnabledStates()
    {
        foreach (var pair in _activeButtons)
            pair.Value.SetEnabled(pair.Key.CanExecute(_bindedMenuElement, _currentTurnState.ActiveAgent));
    }
}
