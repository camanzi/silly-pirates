using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Comodità da Editor: permette di premere Play direttamente su una scena di contenuto (MainMenu o
/// combattimento) caricando la scena persistente se non c'è già, così che camera, audio e VFX
/// esistano comunque.
///
/// Il corpo è racchiuso in UNITY_EDITOR di proposito: nelle build l'unico ingresso è la scena
/// persistente, e non esiste un secondo percorso di avvio da tenere in piedi. Un bootstrap attivo
/// anche in build sarebbe una strada che nessuno percorre mai e che quindi marcisce senza accorgersene.
/// </summary>
[DefaultExecutionOrder(-2000)]
public class PersistentSceneBootstrapper : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField] private SceneReferenceSO _persistentScene;
#endif

    private void Awake()
    {
#if UNITY_EDITOR
        if (_persistentScene == null || !_persistentScene.IsValid)
        {
            Debug.LogError(
                $"[{nameof(PersistentSceneBootstrapper)}] scena persistente non assegnata: premendo " +
                "Play da questa scena mancheranno camera, audio e VFX.", this);
            return;
        }

        if (SceneManager.GetSceneByName(_persistentScene.SceneName).isLoaded) return;

        // Sincrona e non additiva-async: il caricamento si completa comunque a fine frame, quindi i
        // sistemi persistenti sono disponibili dal frame successivo. I consumatori che risolvono via
        // RuntimeAnchorSO reggono già l'attesa (pull-then-subscribe), gli altri devono null-guardare.
        SceneManager.LoadScene(_persistentScene.SceneName, LoadSceneMode.Additive);
#endif
    }
}
