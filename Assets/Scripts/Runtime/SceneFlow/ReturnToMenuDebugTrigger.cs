using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Trigger PROVVISORIO per tornare al menu durante il combattimento, in attesa di una schermata di
/// pausa o di fine combattimento vera. Vive nella scena di combattimento, non nella persistente,
/// così il tasto non fa nulla mentre si è già nel menu.
///
/// Legge la tastiera direttamente invece di passare da InputReader: è codice di servizio destinato a
/// sparire, e aggiungere un'azione a GameInput per qualcosa che verrà rimosso lascerebbe in giro una
/// action orfana.
/// </summary>
public class ReturnToMenuDebugTrigger : MonoBehaviour
{
    [SerializeField] private VoidEventChannel _returnToMenuRequested;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.escapeKey.wasPressedThisFrame)
            _returnToMenuRequested?.RaiseEvent();
    }
}
