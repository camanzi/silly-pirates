using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Main menu. Vive nella scena MainMenu, non nella persistente: ogni scena possiede la propria UI,
/// così non resta stato di schermata da azzerare fra un caricamento e l'altro.
///
/// Non carica nulla da sé: alza un canale e basta. Chi risponde è SceneFlowDirector, nella scena
/// persistente, che è l'unico a sapere cosa è caricato in questo momento.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private UIDocument _document;
    [SerializeField] private VoidEventChannel _startCombatRequested;

    private Button _startButton;

    private void OnEnable()
    {
        _startButton = _document.rootVisualElement.Q<Button>("start-combat-button");

        if (_startButton == null)
        {
            Debug.LogError(
                $"[{nameof(MainMenuController)}] nessun bottone 'start-combat-button' nel documento: " +
                "il menu non può avviare il combattimento.", this);
            return;
        }

        _startButton.clicked += OnStartClicked;
    }

    private void OnDisable()
    {
        if (_startButton != null) _startButton.clicked -= OnStartClicked;
    }

    private void OnStartClicked()
    {
        // Il bottone si spegne al primo click: la scena resta viva ancora qualche frame mentre parte
        // la transizione, e un doppio click alzerebbe il canale due volte.
        _startButton.SetEnabled(false);

        _startCombatRequested?.RaiseEvent();
    }
}
