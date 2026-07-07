using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class TurnCard : VisualElement
{
    private static readonly string USS_CLASS_CARD_CONTAINER = "card-container";
    private static readonly string USS_CLASS_ACTIVE = "turn-card-wrapper--active";
    private static readonly string USS_CLASS_WAITING = "turn-card-wrapper--waiting";
    private static readonly string USS_CLASS_SUB_TURN = "turn-card-wrapper--sub-turn";
    private static readonly string USS_CLASS_SUB_TURN_AV_LABEL = "av-label--sub-turn";

    private VisualElement _cardWrapper;
    private VisualElement _card;
    private VisualElement _iconContainer;
    private Label _avLabel;
    private VisualElement _characterIcon;
    private VisualElement _healthFill;

    private CharacterTurnData _data;
    private bool _isActive;
    private Tween _healthTween;
    private float _healthFillPercent;

    public TurnCard()
    {
        AddToClassList(USS_CLASS_CARD_CONTAINER);
        IsActive = false;
    }

    public void InitElements()
    {
        CacheElements();
        UpdateActiveState();
    }

    private void CacheElements()
    {
        _cardWrapper = this.Q<VisualElement>("card-wrapper");
        _card = this.Q<VisualElement>("card");
        _iconContainer = this.Q<VisualElement>("icon-container");
        _characterIcon = this.Q<VisualElement>("character-icon");
        _healthFill = this.Q<VisualElement>("health-fill");
        _avLabel = this.Q<Label>("AV-label");
    }

    public CharacterTurnData Data
    {
        get => _data;
        set
        {
            _data = value;
            UpdateDisplay();
            UpdateActionValueDisplay();
        }
    }

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive == value) return;
            _isActive = value;
            UpdateActiveState();
            UpdateActionValueDisplay();
        }
    }

    public void Unbind()
    {
        _cardWrapper.style.translate = StyleKeyword.Initial;
        _healthTween.Stop();
    }

    public void UpdateHealth(float currentHp, float maxHp)
    {
        float ratio = maxHp > 0f ? Mathf.Clamp01((maxHp - currentHp) / maxHp) : 1f;
        float targetPercent = ratio * 100f;

        _healthTween.Stop();
        _healthTween = Tween.Custom(_healthFill, _healthFillPercent, targetPercent, duration: 0.3f,
            onValueChange: (target, val) =>
            {
                _healthFillPercent = val;
                target.style.height = Length.Percent(val);
            });
    }

    private void UpdateDisplay()
    {
        if (_data.Icon != null)
        {
            _characterIcon.style.backgroundImage = new StyleBackground(_data.Icon);
        }
        UpdateSubTurnState();
    }

    private void UpdateSubTurnState()
    {
        if (_data.IsSubTurn)
        {
            _cardWrapper.AddToClassList(USS_CLASS_SUB_TURN);
            _avLabel.AddToClassList(USS_CLASS_SUB_TURN_AV_LABEL);
        }
        else
        {
            _cardWrapper.RemoveFromClassList(USS_CLASS_SUB_TURN);
            _avLabel.RemoveFromClassList(USS_CLASS_SUB_TURN_AV_LABEL);
        }
    }

    private void UpdateActiveState()
    {
        _cardWrapper.RemoveFromClassList(USS_CLASS_ACTIVE);
        _cardWrapper.RemoveFromClassList(USS_CLASS_WAITING);

        if (_isActive)
            _cardWrapper.AddToClassList(USS_CLASS_ACTIVE);
        else
            _cardWrapper.AddToClassList(USS_CLASS_WAITING);
    }

    private void UpdateActionValueDisplay()
    {
        _avLabel.text = _isActive ? string.Empty : _data.ActionValue.ToString();;
    }
}
