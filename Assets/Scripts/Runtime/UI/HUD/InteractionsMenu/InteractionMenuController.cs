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

    [Header("Combat Intro")]
    [Tooltip("Se non assegnato il menu si comporta come oggi: nessuna regressione nelle scene senza CombatIntroSequencer.")]
    [SerializeField] private CombatIntroStateSO _introState;
    [Tooltip("Obbligatorio se _introState è assegnato: è l'unico segnale che riapre il menu a fine intro.")]
    [SerializeField] private VoidEventChannel _onCombatStarted;

    private Dictionary<InteractionActionSO, InteractionButton> _activeButtons = new();
    private VisualElement _statusContainer;
    private EquipmentStatusElement _statusElement;
    private Tween _statusVisibilityTween;

    protected override void Awake()
    {
        base.Awake();
        _statusContainer = _uiDocument.rootVisualElement.Q<VisualElement>("status-container");
        _statusContainer.pickingMode = PickingMode.Position;

        if (IsIntroGateActive)
        {
            // Si usa lo slot "element state" e non quello "combat state": quest'ultimo è già guidato
            // dal BoolChannelListener su ShowUIEventChannel, che CombatStateManager.Start() rialza a
            // true nello stesso frame entrando nello stato Idle — il gate dell'intro verrebbe perso.
            _isAllowedByElementState = false;
            ApplyVisibilityImmediate();
        }

        ApplyStatusVisibility(immediate: true);
    }

    private bool IsIntroGateActive
    {
        get
        {
            if (_introState == null || !_introState.IsIntroActive) return false;

            if (_onCombatStarted == null)
            {
                Debug.LogWarning($"{nameof(InteractionMenuController)}: _introState assegnato senza _onCombatStarted, il menu resterebbe nascosto per sempre. Gate ignorato.", this);
                return false;
            }

            return true;
        }
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
            _statusElement = new EquipmentStatusElement();
            _statusContainer.Add(_statusElement);
            SetupStatusElement();
            ApplyStatusVisibility(immediate: true);
        }

        var awakable = _bindedMenuElement as IAwakable;
        if (awakable != null)
        {
            awakable.OnAwakeningCountersChanged += OnAwakeningStateChanged;
            awakable.OnCooldownChanged += OnCooldownStateChanged;
            awakable.OnAwakeningHoverPreview += OnHoverPreview;
        }

        if (_onCombatStarted != null)
            _onCombatStarted.OnEventRaised += HandleCombatStarted;

        // Copre sia le riattivazioni dopo il combat start sia le scene senza sequencer di intro:
        // in entrambi i casi il permesso va concesso da subito.
        if (_introState == null || !_introState.IsIntroActive)
            SetIntroPermission(true);
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        _statusVisibilityTween.Stop();

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

        if (_onCombatStarted != null)
            _onCombatStarted.OnEventRaised -= HandleCombatStarted;
    }

    // Fade-in standard (.25s) quando il combattimento comincia davvero.
    private void HandleCombatStarted() => SetIntroPermission(true);

    private void SetIntroPermission(bool isAllowed)
    {
        SetElementStatePermission(isAllowed);     // menu radiale: gate della base class
        ApplyStatusVisibility(immediate: false);  // anello di stato: gate locale
    }

    /// <summary>
    /// L'anello di stato dell'equipaggiamento vive fuori dal Container gestito da
    /// <see cref="WorldSpaceContainer"/>, quindi nessuno dei permessi della base class lo tocca:
    /// senza questo resterebbe a schermo (e hoverabile) per tutta l'intro. Segue lo stesso flag
    /// _isAllowedByElementState del menu radiale, con lo stesso fade di .25s.
    /// </summary>
    private void ApplyStatusVisibility(bool immediate)
    {
        if (_statusContainer == null) return;

        bool visible = _isAllowedByElementState;
        _statusVisibilityTween.Stop();

        if (immediate)
        {
            _statusContainer.style.opacity = visible ? 1f : 0f;
            _statusContainer.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            return;
        }

        // Il container potrebbe essere a display:None da un fade-out precedente: va ripristinato
        // prima di animare, altrimenti il tween scrive su un elemento che il layout non renderizza.
        if (visible) _statusContainer.style.display = DisplayStyle.Flex;

        _statusVisibilityTween = Tween.Custom(
            _statusContainer.style.opacity.value, visible ? 1f : 0f,
            duration: .25f, ease: Ease.OutQuad,
            onValueChange: newVal => _statusContainer.style.opacity = new StyleFloat(newVal))
            .OnComplete(() => {
                if (!_isAllowedByElementState)
                    _statusContainer.style.display = DisplayStyle.None;
            });
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();

        if (MainCamera == null || _statusContainer == null || _statusContainer.panel == null) return;
        if (_statusContainer.style.display == DisplayStyle.None) return;

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
