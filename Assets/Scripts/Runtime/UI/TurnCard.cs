using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Custom VisualElement per rappresentare una card nel turn order.
/// Usa il sistema moderno [UxmlElement] per essere usabile in UXML.
/// </summary>
[UxmlElement]
public partial class TurnCard : VisualElement
{
    // Classi CSS per lo styling
    private const string USS_CLASS = "turn-card";
    private const string USS_CLASS_ACTIVE = "turn-card--active";
    private const string USS_CLASS_WAITING = "turn-card--waiting";
    private const string USS_CLASS_ICON_CONTAINER = "turn-card__icon-container";
    private const string USS_CLASS_ICON = "turn-card__icon";
    private const string USS_CLASS_INFO = "turn-card__info";
    private const string USS_CLASS_NAME = "turn-card__name";
    private const string USS_CLASS_HP = "turn-card__hp";
    
    // Elementi UI (cached per performance)
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
        AddToClassList(USS_CLASS);
        
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
        // === ICON CONTAINER ===
        iconContainer = new VisualElement
        {
            name = "icon-container"
        };
        iconContainer.AddToClassList(USS_CLASS_ICON_CONTAINER);
        
        // Icona del personaggio
        characterIcon = new VisualElement
        {
            name = "character-icon"
        };
        characterIcon.AddToClassList(USS_CLASS_ICON);
        iconContainer.Add(characterIcon);
        
        // === INFO CONTAINER ===
        var infoContainer = new VisualElement
        {
            name = "info-container"
        };
        infoContainer.AddToClassList(USS_CLASS_INFO);
        
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
        
        // Aggiungi tutto alla card
        Add(iconContainer);
        Add(infoContainer);
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
        
        // Aggiorna testo
        characterNameLabel.text = _data.characterName;
        hpLabel.text = $"{_data.currentHP}";
        
        // Aggiorna icona
        if (_data.characterIcon != null)
        {
            characterIcon.style.backgroundImage = new StyleBackground(_data.characterIcon);
        }
        
        // Aggiorna colore HP in base alla percentuale
        UpdateHPColor();
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
    /// Colora l'HP in base alla percentuale rimanente
    /// </summary>
    private void UpdateHPColor()
    {
        if (_data == null || _data.maxHP == 0) return;
        
        float hpPercent = (float)_data.currentHP / _data.maxHP;
        
        if (hpPercent <= 0.25f)
        {
            // Critico - rosso
            hpLabel.style.color = new Color(1f, 0.3f, 0.3f);
        }
        else if (hpPercent <= 0.5f)
        {
            // Basso - giallo
            hpLabel.style.color = new Color(1f, 0.8f, 0.3f);
        }
        else
        {
            // Ok - bianco
            hpLabel.style.color = new Color(0.9f, 0.9f, 0.9f);
        }
    }
    
    /// <summary>
    /// Anima il danno ricevuto
    /// </summary>
    public void AnimateDamage(int damage)
    {
        // Applica classe per animazione shake
        AddToClassList("turn-card--damaged");
        
        // Rimuovi la classe dopo 500ms
        schedule.Execute(() => {
            RemoveFromClassList("turn-card--damaged");
        }).StartingIn(500);
        
        // Aggiorna HP
        if (_data != null)
        {
            _data.currentHP = Mathf.Max(0, _data.currentHP - damage);
            UpdateDisplay();
        }
    }
    
    /// <summary>
    /// Anima la guarigione
    /// </summary>
    public void AnimateHeal(int healAmount)
    {
        AddToClassList("turn-card--healed");
        
        schedule.Execute(() => {
            RemoveFromClassList("turn-card--healed");
        }).StartingIn(500);
        
        if (_data != null)
        {
            _data.currentHP = Mathf.Min(_data.maxHP, _data.currentHP + healAmount);
            UpdateDisplay();
        }
    }
}