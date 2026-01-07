using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class TurnCard : VisualElement
{
    private readonly string TEMPLATE_PATH = "UI/Templates/TurnCard/TurnCardTemplate";

    // Classi CSS per lo styling
    private const string USS_CLASS_CARD_CONTAINER = "card-container";
    private const string USS_CLASS_ACTIVE = "turn-card--active";
    private const string USS_CLASS_WAITING = "turn-card--waiting";
    
    // Elementi UI (cached per performance)
    private VisualElement card;
    private VisualElement iconContainer;
    private VisualElement characterIcon;
    
    // Dati interni
    private CharacterTurnData _data;
    private bool _isActive;
    
    /// <summary>
    /// Costruttore - crea la struttura della card
    /// </summary>
    public TurnCard()
    {        
        LoadTemplate();
        CacheElements();
        UpdateActiveState();

        // Stato iniziale
        IsActive = false;
    }
    
    /// <summary>
    /// Carica il template UXML da Resources
    /// </summary>
    private void LoadTemplate()
    {
        VisualTreeAsset template = Resources.Load<VisualTreeAsset>(TEMPLATE_PATH);
        
        if (template == null)
        {
            Debug.LogError($"Template UXML non trovato in Resources/{TEMPLATE_PATH}");
            throw new System.Exception($"Template UXML non trovato in Resources/{TEMPLATE_PATH}");
        }
        
        template.CloneTree(this);        
        AddToClassList(USS_CLASS_CARD_CONTAINER);
    }

    private void CacheElements()
    {
        card = this.Q<VisualElement>("card");
        iconContainer = this.Q<VisualElement>("icon-container");
        characterIcon = this.Q<VisualElement>("character-icon");
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
        }
    }
    
    /// <summary>
    /// Aggiorna il contenuto visualizzato
    /// </summary>
    private void UpdateDisplay()
    {
        if (_data.icon != null)
        {
            characterIcon.style.backgroundImage = new StyleBackground(_data.icon);
        }
    }
    
    /// <summary>
    /// Cambia le classi CSS in base allo stato attivo
    /// </summary>
    private void UpdateActiveState()
    {
        card.RemoveFromClassList(USS_CLASS_ACTIVE);
        card.RemoveFromClassList(USS_CLASS_WAITING);
        
        if (_isActive)
        {
            card.AddToClassList(USS_CLASS_ACTIVE);
        }
        else
        {
            card.AddToClassList(USS_CLASS_WAITING);
        }
    }
}