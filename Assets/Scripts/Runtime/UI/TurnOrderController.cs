using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Controller che gestisce la turn order queue con pooling automatico
/// </summary>
public class TurnOrderController : MonoBehaviour
{   
    [Header("Test Data")]
    [SerializeField] private List<CharacterTurnData> initialTurnQueue = new List<CharacterTurnData>();
    
    [Header("Settings")]
    [SerializeField] private bool autoAdvanceTurns = false;
    [SerializeField] private float turnDuration = 3f;
    
    // UI Elements
    private UIDocument _uiDocument;
    private VisualElement root;
    private ListView turnListView;
    
    // Runtime data
    private List<CharacterTurnData> turnQueue = new List<CharacterTurnData>();
    private float turnTimer;

    void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        // Ottieni root element
        root = _uiDocument.rootVisualElement;
        
        // Trova il ListView nel UXML
        turnListView = root.Q<ListView>("turn-list");
        
        if (turnListView == null)
        {
            Debug.LogError("ListView 'turn-list' non trovato nel UXML!");
            return;
        }
        
        // Setup della lista
        SetupListView();
        
        // Carica dati iniziali
        LoadInitialData();
    }
    
    /// <summary>
    /// Configura il ListView con pooling
    /// </summary>
    private void SetupListView()
    {
        // === MAKE ITEM ===
        // Viene chiamato solo poche volte per creare il pool (6-8 elementi)
        // Unity riutilizza questi elementi automaticamente
        turnListView.makeItem = () => {
            // Semplicemente crea una nuova TurnCard
            // Tutto il setup della struttura è dentro TurnCard stesso
            return new TurnCard();
        };
        
        // === BIND ITEM ===
        // Viene chiamato ogni volta che un elemento viene riciclato
        // O quando chiamiamo RefreshItems()
        turnListView.bindItem = (element, index) => {
            // Cast a TurnCard
            var card = element as TurnCard;
            if (card == null)
            {
                Debug.LogWarning("Element non è una TurnCard!");
                return;
            }
            
            // Controlla bounds
            if (index < 0 || index >= turnQueue.Count) return;
            
            // Imposta i dati
            card.Data = turnQueue[index];
            
            // Il primo elemento è sempre quello attivo (turno corrente)
            card.IsActive = (index == 0);
        };
        
        // === UNBIND ITEM === (opzionale)
        // Chiamato quando un elemento sta per essere riciclato
        turnListView.unbindItem = (element, index) => {
            // Puoi fare cleanup qui se necessario
            // Es: cancellare eventi, fermare animazioni, etc.
        };
        
        // === CONFIGURAZIONE ===
        // fixedItemHeight è CRUCIALE per le performance!
        // Unity sa esattamente quanto spazio occupa ogni elemento
        turnListView.fixedItemHeight = 80; 
        
        // Disabilita selezione (non serve per turn order)
        turnListView.selectionType = SelectionType.None;
        
        // Altezza massima (mostra ~5 card alla volta)
        turnListView.style.height = 420;
        
        // Imposta la lista vuota inizialmente
        turnListView.itemsSource = turnQueue;
    }
    
    /// <summary>
    /// Carica dati di test iniziali
    /// </summary>
    private void LoadInitialData()
    {
        // Se hai dati serializzati, usali
        if (initialTurnQueue != null && initialTurnQueue.Count > 0)
        {
            turnQueue = new List<CharacterTurnData>(initialTurnQueue);
        }
        else
        {
            // Altrimenti crea dati di test
            CreateTestData();
        }
        
        // Ordina per initiative (dal più alto al più basso)
        SortByInitiative();
        
        // Refresh della lista
        RefreshList();
    }
    
    /// <summary>
    /// Crea dati di test se non ci sono dati serializzati
    /// </summary>
    private void CreateTestData()
    {
        // Nota: dovrai assegnare gli sprite nell'Inspector
        turnQueue.Add(new CharacterTurnData("Warrior", null, 100, 100, 18));
        turnQueue.Add(new CharacterTurnData("Mage", null, 60, 80, 15));
        turnQueue.Add(new CharacterTurnData("Rogue", null, 70, 70, 20));
        turnQueue.Add(new CharacterTurnData("Cleric", null, 85, 90, 12));
        turnQueue.Add(new CharacterTurnData("Goblin", null, 25, 30, 10));
        turnQueue.Add(new CharacterTurnData("Orc", null, 45, 50, 8));
    }
    
    /// <summary>
    /// Ordina la queue per initiative
    /// </summary>
    private void SortByInitiative()
    {
        turnQueue = turnQueue.OrderByDescending(c => c.initiative).ToList();
    }
    
    /// <summary>
    /// Refresh completo della lista
    /// </summary>
    private void RefreshList()
    {
        // Imposta nuovamente l'itemsSource (forza rebuild completo)
        turnListView.itemsSource = turnQueue;
        
        // Oppure usa RefreshItems() se l'itemsSource è già impostato
        turnListView.RefreshItems();
        
        // Scroll alla cima
        turnListView.ScrollToItem(0);
    }
    
    /// <summary>
    /// Avanza al turno successivo
    /// </summary>
    public void AdvanceTurn()
    {
        if (turnQueue.Count == 0) return;
        
        // Prendi il primo (turno corrente)
        var currentTurn = turnQueue[0];
        
        // Rimuovilo dalla cima
        turnQueue.RemoveAt(0);
        
        // Rimettilo in fondo
        turnQueue.Add(currentTurn);
        
        // MAGIA DEL POOLING:
        // RefreshItems() fa rebind di tutti gli elementi visibili
        // Gli stessi VisualElement vengono riutilizzati, ma con nuovi dati
        // Le transizioni CSS fanno l'animazione automaticamente!
        turnListView.RefreshItems();
        
        Debug.Log($"Turno avanzato. Ora tocca a: {turnQueue[0].characterName}");
    }
    
    /// <summary>
    /// Applica danno a un personaggio
    /// </summary>
    public void DamageCharacter(string characterName, int damage)
    {
        var index = turnQueue.FindIndex(c => c.characterName == characterName);
        if (index < 0)
        {
            Debug.LogWarning($"Personaggio {characterName} non trovato!");
            return;
        }
        
        // Aggiorna i dati
        turnQueue[index].currentHP = Mathf.Max(0, turnQueue[index].currentHP - damage);
        
        // Refresh solo quella card
        turnListView.RefreshItem(index);
        
        Debug.Log($"{characterName} prende {damage} danni! HP: {turnQueue[index].currentHP}");
        
        // Se è morto, rimuovilo
        if (turnQueue[index].currentHP <= 0)
        {
            RemoveCharacter(characterName);
        }
    }
    
    /// <summary>
    /// Guarisce un personaggio
    /// </summary>
    public void HealCharacter(string characterName, int healAmount)
    {
        var index = turnQueue.FindIndex(c => c.characterName == characterName);
        if (index < 0) return;
        
        var data = turnQueue[index];
        data.currentHP = Mathf.Min(data.maxHP, data.currentHP + healAmount);
        
        turnListView.RefreshItem(index);
        
        Debug.Log($"{characterName} viene curato di {healAmount}! HP: {data.currentHP}");
    }
    
    /// <summary>
    /// Rimuove un personaggio dalla queue (morte)
    /// </summary>
    public void RemoveCharacter(string characterName)
    {
        var index = turnQueue.FindIndex(c => c.characterName == characterName);
        if (index < 0) return;
        
        turnQueue.RemoveAt(index);
        RefreshList();
        
        Debug.Log($"{characterName} è stato sconfitto!");
    }
    
    /// <summary>
    /// Aggiunge un nuovo personaggio alla queue
    /// </summary>
    public void AddCharacter(CharacterTurnData newCharacter)
    {
        turnQueue.Add(newCharacter);
        SortByInitiative();
        RefreshList();
    }
    
    private void Update()
    {
        // Auto-advance per test
        if (autoAdvanceTurns)
        {
            turnTimer += Time.deltaTime;
            if (turnTimer >= turnDuration)
            {
                turnTimer = 0;
                AdvanceTurn();
            }
        }
    }
    
    // === METODI DI TEST (chiamabili dall'Inspector) ===
    
    [ContextMenu("Advance Turn")]
    private void TestAdvanceTurn()
    {
        AdvanceTurn();
    }
    
    [ContextMenu("Damage First Character")]
    private void TestDamage()
    {
        if (turnQueue.Count > 0)
        {
            DamageCharacter(turnQueue[0].characterName, 15);
        }
    }
    
    [ContextMenu("Heal First Character")]
    private void TestHeal()
    {
        if (turnQueue.Count > 0)
        {
            HealCharacter(turnQueue[0].characterName, 20);
        }
    }
}