using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using System;

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
            TurnCard card = element as TurnCard;
            if (card == null)
            {
                Debug.LogWarning("Element non è una TurnCard!");
                return;
            }
            
            if (index < 0 || index >= turnQueue.Count) return;
            
            card.Data = turnQueue[index];
            
            card.IsActive = index == 0;
        };
        
        // === UNBIND ITEM === (opzionale)
        // Chiamato quando un elemento sta per essere riciclato
        turnListView.unbindItem = (element, index) => {
            // Puoi fare cleanup qui se necessario
            // Es: cancellare eventi, fermare animazioni, etc.
        };
        
        // Unity sa esattamente quanto spazio occupa ogni elemento
        turnListView.fixedItemHeight = 100; 
        
        // Disabilita selezione (non serve per turn order)
        turnListView.selectionType = SelectionType.None;
        
        // Imposta la lista vuota inizialmente
        turnListView.itemsSource = turnQueue;
    }
    
    /// <summary>
    /// Carica dati di test iniziali
    /// </summary>
    private void LoadInitialData()
    {
        turnQueue = new List<CharacterTurnData>(initialTurnQueue);
           
        // Refresh della lista
        RefreshList();
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
    }
    
    /// <summary>
    /// Applica danno a un personaggio
    /// </summary>
    public void DamageCharacter(Guid characterID)
    {
        int index = turnQueue.FindIndex(c => c.characterID == characterID);
        if (index < 0)
        {
            Debug.LogWarning($"Personaggio {characterID} non trovato!");
            return;
        }
        
        // Refresh solo quella card
        turnListView.RefreshItem(index);
        
        Debug.Log($"{characterID} é stato danneggiato!");
    }
    
    /// <summary>
    /// Rimuove un personaggio dalla queue (morte)
    /// </summary>
    public void RemoveCharacter(Guid characterID)
    {
        var index = turnQueue.FindIndex(c => c.characterID == characterID);
        if (index < 0) return;
        
        turnQueue.RemoveAt(index);
        RefreshList();
        
        Debug.Log($"{characterID} è stato sconfitto!");
    }
    
    /// <summary>
    /// Aggiunge un nuovo personaggio alla queue
    /// </summary>
    public void AddCharacter(CharacterTurnData newCharacter)
    {
        turnQueue.Add(newCharacter);
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
            DamageCharacter(turnQueue[0].characterID);
        }
    }
}