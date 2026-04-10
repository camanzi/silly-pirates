using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class TurnCard : VisualElement
{
    // Classi CSS per lo styling
    private const string USS_CLASS_CARD_CONTAINER = "card-container";
    private const string USS_CLASS_ACTIVE = "turn-card-wrapper--active";
    private const string USS_CLASS_WAITING = "turn-card-wrapper--waiting";
    
    // Elementi UI (cached per performance)
    private VisualElement _cardWrapper;
    private VisualElement _card;
    private VisualElement _iconContainer;
    private Label _avLabel;
    private VisualElement _characterIcon;
    
    // Dati interni
    private CharacterTurnData _data;
    private bool _isActive;
    
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
    
    /// <summary>
    /// Aggiorna il contenuto visualizzato
    /// </summary>
    private void UpdateDisplay()
    {
        if (_data.Icon != null)
        {
            _characterIcon.style.backgroundImage = new StyleBackground(_data.Icon);
        }
    }
    
    /// <summary>
    /// Cambia le classi CSS in base allo stato attivo
    /// </summary>
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
}