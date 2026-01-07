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
    
    private UIDocument _uiDocument;
    private VisualElement root;
    private ListView turnListView;
    
    private List<CharacterTurnData> turnQueue = new List<CharacterTurnData>();
    private float turnTimer;

    void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        root = _uiDocument.rootVisualElement;
        
        turnListView = root.Q<ListView>("turn-list");
        
        if (turnListView == null)
        {
            Debug.LogError("ListView 'turn-list' non trovato nel UXML!");
            return;
        }
        
        SetupListView();
        LoadInitialData();
    }
    
    /// <summary>
    /// Configura il ListView con pooling
    /// </summary>
    private void SetupListView()
    {
        turnListView.makeItem = () => { return new TurnCard(); };
        
        turnListView.bindItem = (element, index) => {
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
        
        // Chiamato quando un elemento sta per essere riciclato
        turnListView.unbindItem = (element, index) => {};
        
        turnListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
        turnListView.selectionType = SelectionType.None;
        turnListView.itemsSource = turnQueue;
    }
    
    /// <summary>
    /// Carica dati di test iniziali
    /// </summary>
    private void LoadInitialData()
    {
        turnQueue = new List<CharacterTurnData>(initialTurnQueue);
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
        
        CharacterTurnData currentTurn = turnQueue[0];        
        turnQueue.RemoveAt(0);
        turnQueue.Add(currentTurn);
        turnListView.RefreshItems();
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
}