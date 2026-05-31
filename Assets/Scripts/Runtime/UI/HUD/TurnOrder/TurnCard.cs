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

    private CharacterTurnData _data;
    private bool _isActive;
    private bool _isHovered;
    private Tween _wobbleTween;
    private Tween _shakeTween;

    private const float WobbleAmount = 15f;
    private const float WobbleDuration = .75f;
    private const float ShakeAmplitude = 20f;
    private const float ShakeDuration = 0.8f;
    private const int ShakeCycles = 4;

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
        _avLabel = this.Q<Label>("AV-label");
    }

    public CharacterTurnData Data
    {
        get => _data;
        set
        {
            if (_data?.Agent?.Health != null)
                _data.Agent.Health.OnTakeDamage -= PlayDamageShake;
            _data = value;
            if (_data?.Agent?.Health != null)
                _data.Agent.Health.OnTakeDamage += PlayDamageShake;
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
        if (_data?.Agent?.Health != null)
            _data.Agent.Health.OnTakeDamage -= PlayDamageShake;
        Tween.StopAll(this);
        _cardWrapper.style.translate = StyleKeyword.Initial;
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

    public void SetHovered(bool hovered)
    {
        _isHovered = hovered;
        if (_shakeTween.isAlive) return;
        Tween.StopAll(this);
        _cardWrapper.style.translate = StyleKeyword.Initial;
        if (!hovered) return;
        _wobbleTween = Tween.Custom(this, 0, WobbleAmount, WobbleDuration,
            (self, v) => self._cardWrapper.style.translate = new StyleTranslate(new Translate(v, 0f)),
            cycles: -1, cycleMode: CycleMode.Rewind, ease: Ease.InOutSine);
    }

    private void PlayDamageShake()
    {
        Tween.StopAll(this);
        _shakeTween = Tween.Custom(this, 0f, 1f, ShakeDuration, (self, t) =>
        {
            float shake = Mathf.Sin(t * Mathf.PI * 2f * ShakeCycles) * (1f - t) * ShakeAmplitude;
            self._cardWrapper.style.translate = new StyleTranslate(new Translate(shake, 0f));
        }, ease: Ease.Linear).OnComplete(() => SetHovered(_isHovered));
    }
}
