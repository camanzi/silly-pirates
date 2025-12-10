using UnityEngine;
using UnityEngine.UIElements;

// Bisogna far si che l'UXML non venga creato a mano lato script, ma caricato come Resource esterno
[UxmlElement]
public partial class TurnCard : VisualElement
{
    // Classi CSS per lo styling
    private const string USS_CLASS = "turn-card";
    private const string USS_CLASS_CARD_CONTAINER = "card-container";
    private const string USS_CLASS_ACTIVE = "turn-card--active";
    private const string USS_CLASS_WAITING = "turn-card--waiting";
    private const string USS_CLASS_ICON_CONTAINER = "turn-card__icon-container";
    private const string USS_CLASS_ICON = "turn-card__icon";
    private const string USS_CLASS_INFO = "turn-card__info";
    private const string USS_CLASS_NAME = "turn-card__name";
    private const string USS_CLASS_HP = "turn-card__hp";
    
    // Elementi UI (cached per performance)
    private VisualElement cardContainer;
    private VisualElement iconContainer;
    private VisualElement characterIcon;
    private Label characterNameLabel;
    private Label hpLabel;
    
    // Dati interni
    private CharacterTurnData _data;
    private bool _isActive;
    
    /// <summary>
    /// Costruttore - crea la struttura della card
    /// </summary>
    public TurnCard()
    {
        // Aggiungi classe base
        AddToClassList(USS_CLASS_CARD_CONTAINER);
        
        // Costruisci la struttura interna
        BuildStructure();
        
        // Stato iniziale
        IsActive = false;
    }
    
    /// <summary>
    /// Costruisce la gerarchia di elementi
    /// </summary>
    private void BuildStructure()
    {
        // === CARD CONTAINER ====
        cardContainer = new VisualElement
        {
            name = "card-container"
        };
        cardContainer.AddToClassList(USS_CLASS);

        // === ICON CONTAINER ===
        iconContainer = new VisualElement
        {
            name = "icon-container",
        };
        iconContainer.AddToClassList(USS_CLASS_ICON_CONTAINER);
        cardContainer.Add(iconContainer);

        // Icona del personaggio
        characterIcon = new VisualElement
        {
            name = "character-icon"
        };
        characterIcon.AddToClassList(USS_CLASS_ICON);
        iconContainer.Add(characterIcon);
        
        // === INFO CONTAINER ===
        VisualElement infoContainer = new VisualElement
        {
            name = "info-container"
        };
        infoContainer.AddToClassList(USS_CLASS_INFO);
        cardContainer.Add(infoContainer);
        
        // Nome
        characterNameLabel = new Label
        {
            name = "character-name",
            text = ""
        };
        characterNameLabel.AddToClassList(USS_CLASS_NAME);
        
        // HP
        hpLabel = new Label
        {
            name = "character-hp",
            text = ""
        };
        hpLabel.AddToClassList(USS_CLASS_HP);
        
        infoContainer.Add(characterNameLabel);
        infoContainer.Add(hpLabel);
        
        Add(cardContainer);
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
        if (_data == null)
        {
            characterNameLabel.text = "";
            hpLabel.text = "";
            return;
        }
        
        // Aggiorna icona
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
        // IMPORTANTE: Rimuovi entrambe le classi prima
        // (l'elemento potrebbe essere riciclato dal pooling)
        RemoveFromClassList(USS_CLASS_ACTIVE);
        RemoveFromClassList(USS_CLASS_WAITING);
        
        // Aggiungi la classe corretta
        if (_isActive)
        {
            AddToClassList(USS_CLASS_ACTIVE);
        }
        else
        {
            AddToClassList(USS_CLASS_WAITING);
        }
    }
    
    /// <summary>
    /// Anima il danno ricevuto
    /// </summary>
    public void AnimateDamage()
    {
        // Applica classe per animazione shake
        AddToClassList("turn-card--damaged");
        
        schedule.Execute(() => {
            RemoveFromClassList("turn-card--damaged");
        }).StartingIn(500);
    }
}