using PrimeTween;
using UnityEngine.UIElements;

[UxmlElement]
public partial class TurnCard : VisualElement
{
    // Classi CSS per lo styling
    private static readonly string USS_CLASS_CARD_CONTAINER = "card-container";
    private static readonly string USS_CLASS_ACTIVE = "turn-card-wrapper--active";
    private static readonly string USS_CLASS_WAITING = "turn-card-wrapper--waiting";
    private static readonly string USS_CLASS_SUB_TURN = "turn-card-wrapper--sub-turn";
    private static readonly string USS_CLASS_SUB_TURN_AV_LABEL = "av-label--sub-turn";
    
    // Elementi UI (cached per performance)
    private VisualElement _cardWrapper;
    private VisualElement _card;
    private VisualElement _iconContainer;
    private Label _avLabel;
    private VisualElement _characterIcon;
    
    // Dati interni
    private CharacterTurnData _data;
    private bool _isActive;
    private Tween _wobbleTween;

    private const float WobbleAmount = 15f;
    private const float WobbleDuration = .75f;
    
    /// <summary>
    /// Costruttore - crea la struttura della card
    /// </summary>
    public TurnCard()
    {   
        AddToClassList(USS_CLASS_CARD_CONTAINER);

        // Stato iniziale
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
    
    /// <summary>
    /// Dati del personaggio da visualizzare
    /// </summary>
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
    
    /// <summary>
    /// Se true, questa è la card del turno corrente (più grande, animata)
    /// </summary>
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
        {
            _cardWrapper.AddToClassList(USS_CLASS_ACTIVE);
        }
        else
        {
            _cardWrapper.AddToClassList(USS_CLASS_WAITING);
        }
    }

    private void UpdateActionValueDisplay()
    {
        _avLabel.text = _isActive ? string.Empty : _data.ActionValue.ToString();;
    }

    public void SetHovered(bool hovered)
    {
        _wobbleTween.Stop();
        _cardWrapper.style.translate = StyleKeyword.Initial;
        if (!hovered) return;
        _wobbleTween = Tween.Custom(0, WobbleAmount, WobbleDuration,
            onValueChange: v => _cardWrapper.style.translate = new StyleTranslate(new Translate(v, 0f)),
            cycles: -1, cycleMode: CycleMode.Rewind, ease: Ease.InOutSine);
    }
}