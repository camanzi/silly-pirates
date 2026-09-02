using System;
using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

public class ActiveCharacterPanelController : MonoBehaviour
{
    [Header("Dependences")]
    [SerializeField] private UIDocument _hudDocument;
    [SerializeField] private VisualTreeAsset _passiveIndicatorTemplate;
    [SerializeField] private PassiveHoverPopupController _hoverPopup;

    private VisualElement _root;
    private VisualElement _portrait;
    private VisualElement _nativePassiveSlot;
    private VisualElement _hpBarFill;
    private Label _hpLabel;
    private VisualElement _hpBarBg;

    private HealthController _cachedHealth;
    private PassiveIndicator _nativePassiveIndicator;
    private IPassiveStateNotifier _nativePassiveNotifier;
    private Action _nativePassiveHandler;
    private PassiveAbilityController _cachedPassiveController;

    private Tween _hpTween;
    // Percentuale corrente del fill: il tween deve partire da qui e non da resolvedStyle.width,
    // che e' in pixel (unit mismatch -> la barra sbordava dal container per tutta la durata del tween).
    private float _hpFillPercent = 100f;

    private void Awake()
    {
        var root = _hudDocument.rootVisualElement;
        _root = root.Q<VisualElement>("active-character-panel");
        _portrait = root.Q<VisualElement>("acp-portrait");
        _nativePassiveSlot = root.Q<VisualElement>("native-passive-slot");
        _hpBarFill = root.Q<VisualElement>("hp-bar-fill");
        _hpLabel = root.Q<Label>("hp-label");
        _hpBarBg = root.Q<VisualElement>("hp-bar-bg");
    }

    public void HandleAgentActivated(ITurnAgent agent)
    {
        UnsubscribeFromAgent();

        if (agent == null || !agent.CompareTag("Player"))
        {
            _hpTween.Stop();
            _root.style.display = DisplayStyle.None;
            _nativePassiveSlot.Clear();
            return;
        }

        _root.style.display = DisplayStyle.Flex;

        if (agent.RenderingData != null && agent.RenderingData.TurnAgentIcon != null)
            _portrait.style.backgroundImage = new StyleBackground(agent.RenderingData.TurnAgentIcon);

        _cachedHealth = agent.Health;
        if (_cachedHealth != null)
        {
            _cachedHealth.OnHpChanged += HandleHpChanged;
            // Snap: al cambio di character la barra si posiziona subito, senza animazione.
            SetHealth(_cachedHealth.CurrentHp, _cachedHealth.MaxHp, animate: false);
        }

        RefreshNativePassive(agent);
    }

    private void RefreshNativePassive(ITurnAgent agent)
    {
        _nativePassiveSlot.Clear();
        _nativePassiveIndicator = null;

        if (agent is Component comp && comp.TryGetComponent<PassiveAbilityController>(out var pc) && pc.NativePassive != null)
        {
            _cachedPassiveController = pc;

            _nativePassiveIndicator = new PassiveIndicator(pc.NativePassive, _passiveIndicatorTemplate, _hoverPopup, PassiveIndicatorSize.Large);
            _nativePassiveSlot.Add(_nativePassiveIndicator);
            ApplyNativePassiveVisualState(pc.NativePassive, pc);

            if (pc.NativePassive is IPassiveStateNotifier notifier)
            {
                _nativePassiveNotifier = notifier;
                _nativePassiveHandler = () => ApplyNativePassiveVisualState(pc.NativePassive, pc);
                _nativePassiveNotifier.OnStateChanged += _nativePassiveHandler;
            }
        }
        else
        {
            _cachedPassiveController = null;
        }
    }

    private void ApplyNativePassiveVisualState(PassiveAbilitySO passive, PassiveAbilityController controller)
    {
        if (_nativePassiveIndicator == null) return;

        if (passive is IStackCountProvider stackProvider)
        {
            _nativePassiveIndicator.style.opacity = 1f;
            _nativePassiveIndicator.UpdateStackBadge(stackProvider.CurrentStacks, stackProvider.MaxStacks, alwaysShow: true);
        }
        else
        {
            bool isActive = passive.IsCurrentlyActive(controller);
            _nativePassiveIndicator.style.opacity = isActive ? 1f : 0.4f;
        }
    }

    private void HandleHpChanged(float currentHp)
    {
        if (_cachedHealth == null) return;
        SetHealth(currentHp, _cachedHealth.MaxHp, animate: true);
    }

    private void SetHealth(float currentHp, float maxHp, bool animate)
    {
        float ratio = maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;
        float targetPercent = ratio * 100f;
        _hpLabel.text = $"{Mathf.CeilToInt(currentHp)}/{Mathf.CeilToInt(maxHp)}";

        _hpTween.Stop();

        if (!animate)
        {
            _hpFillPercent = targetPercent;
            _hpBarFill.style.width = Length.Percent(targetPercent);
            return;
        }

        _hpTween = Tween.Custom(_hpBarFill, _hpFillPercent, targetPercent, duration: 0.3f,
            onValueChange: (target, val) =>
            {
                _hpFillPercent = val;
                target.style.width = Length.Percent(val);
            });
    }

    private void UnsubscribeFromAgent()
    {
        if (_cachedHealth != null)
        {
            _cachedHealth.OnHpChanged -= HandleHpChanged;
            _cachedHealth = null;
        }

        if (_nativePassiveNotifier != null && _nativePassiveHandler != null)
        {
            _nativePassiveNotifier.OnStateChanged -= _nativePassiveHandler;
        }
        _nativePassiveNotifier = null;
        _nativePassiveHandler = null;
        _cachedPassiveController = null;
    }

    private void OnDestroy()
    {
        UnsubscribeFromAgent();
    }
}
