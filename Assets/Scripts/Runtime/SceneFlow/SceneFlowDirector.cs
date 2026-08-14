using System;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Orchestratore delle transizioni fra le scene di contenuto (MainMenu e combattimento), che vengono
/// caricate in additivo sopra la scena persistente. Vive nella persistente e non viene mai scaricato.
///
/// L'ordine delle tre operazioni non è negoziabile: SCARICO della scena vecchia, poi RESET dello
/// stato di sessione, poi CARICO della nuova. Resettare dopo il carico cancellerebbe le
/// registrazioni appena fatte dagli OnEnable della scena entrante (occupancy della griglia, agenti
/// nella coda dei turni), che è esattamente il bug che il reset dovrebbe prevenire.
/// </summary>
public class SceneFlowDirector : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private SceneReferenceSO _mainMenuScene;
    [SerializeField] private SceneReferenceSO _combatScene;

    [Header("Sessione")]
    [Tooltip("Elenco degli SO con stato runtime azzerati fra uno scarico e il carico successivo.")]
    [SerializeField] private CombatSessionSO _combatSession;

    [Header("Canali")]
    [SerializeField] private VoidEventChannel _startCombatRequested;
    [SerializeField] private VoidEventChannel _returnToMenuRequested;
    [SerializeField] private FloatEventChannel _loadingProgress;
    [SerializeField] private BoolEventChannel _loadingVisible;

    [Header("Config")]
    [Tooltip("Permanenza minima dell'overlay. Le scene qui sono piccole e senza questo la barra " +
             "lampeggerebbe senza che si riesca a leggere la percentuale.")]
    [Min(0f)] [SerializeField] private float _minimumDisplaySeconds = 0.75f;

    private const float LoadWatchdogSeconds = 30f;

    // Scena di contenuto attualmente caricata. La persistente non compare mai qui: non si scarica.
    private string _currentSceneName;
    private bool _isTransitioning;

    private void OnEnable()
    {
        if (_startCombatRequested != null) _startCombatRequested.OnEventRaised += HandleStartCombat;
        if (_returnToMenuRequested != null) _returnToMenuRequested.OnEventRaised += HandleReturnToMenu;
    }

    private void OnDisable()
    {
        if (_startCombatRequested != null) _startCombatRequested.OnEventRaised -= HandleStartCombat;
        if (_returnToMenuRequested != null) _returnToMenuRequested.OnEventRaised -= HandleReturnToMenu;
    }

    private void Start()
    {
        // Se una scena di contenuto è già caricata siamo stati avviati dal PersistentSceneBootstrapper
        // (Play diretto su una scena di gameplay in Editor): la si adotta invece di caricare il menu
        // sopra, che darebbe due scene di contenuto vive insieme.
        if (TryAdoptLoadedContentScene()) return;

        _ = TransitionToAsync(_mainMenuScene, destroyCancellationToken);
    }

    private bool TryAdoptLoadedContentScene()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            if (Matches(scene, _combatScene) || Matches(scene, _mainMenuScene))
            {
                _currentSceneName = scene.name;
                return true;
            }
        }

        return false;
    }

    private static bool Matches(Scene scene, SceneReferenceSO reference) =>
        reference != null && reference.IsValid && scene.name == reference.SceneName;

    private void HandleStartCombat() => _ = TransitionToAsync(_combatScene, destroyCancellationToken);

    private void HandleReturnToMenu() => _ = TransitionToAsync(_mainMenuScene, destroyCancellationToken);

    private async Awaitable TransitionToAsync(SceneReferenceSO target, CancellationToken token)
    {
        if (target == null || !target.IsValid)
        {
            Debug.LogError($"[{nameof(SceneFlowDirector)}] scena di destinazione non assegnata o senza nome.", this);
            return;
        }

        // Un secondo click sul bottone mentre la transizione è in corso scaricherebbe una scena a
        // metà caricamento: si ignora invece di accodare.
        if (_isTransitioning) return;
        _isTransitioning = true;

        try
        {
            _loadingVisible?.RaiseEvent(true);
            _loadingProgress?.RaiseEvent(0f);

            // unscaledTime: una transizione non deve dipendere dal timeScale del gioco.
            float overlayShownAt = Time.unscaledTime;

            // Si cede sempre almeno un frame prima di toccare le scene, così questo metodo non può
            // mai completarsi in modo sincrono e l'overlay ha un frame per disegnarsi.
            await Awaitable.NextFrameAsync(token);

            await UnloadCurrentSceneAsync(token);

            _combatSession?.ResetForNewCombat();

            await LoadSceneAsync(target, overlayShownAt, token);

            _loadingVisible?.RaiseEvent(false);
        }
        catch (OperationCanceledException)
        {
            // Director distrutto a metà transizione (uscita dal Play Mode): nulla da riportare.
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    private async Awaitable UnloadCurrentSceneAsync(CancellationToken token)
    {
        if (string.IsNullOrEmpty(_currentSceneName)) return;

        Scene current = SceneManager.GetSceneByName(_currentSceneName);
        _currentSceneName = null;

        if (!current.IsValid() || !current.isLoaded) return;

        AsyncOperation unload = SceneManager.UnloadSceneAsync(current);
        if (unload == null) return;

        while (!unload.isDone)
            await Awaitable.NextFrameAsync(token);
    }

    private async Awaitable LoadSceneAsync(SceneReferenceSO target, float overlayShownAt, CancellationToken token)
    {
        AsyncOperation load = SceneManager.LoadSceneAsync(target.SceneName, LoadSceneMode.Additive);
        if (load == null)
        {
            Debug.LogError(
                $"[{nameof(SceneFlowDirector)}] '{target.SceneName}' non è caricabile: manca dalla " +
                "lista in Build Settings?", this);
            return;
        }

        // Con l'attivazione bloccata Unity ferma progress a 0.9: si rinormalizza per avere una
        // percentuale che arriva davvero a 100 invece che a 90.
        load.allowSceneActivation = false;

        // Un unico loop guidato da isDone. La versione precedente attendeva prima "progress >= 0.9"
        // e poi "isDone" in due cicli separati, e si appendeva: il confronto secco con 0.9 non è
        // affidabile (il valore può assestarsi appena sotto) e i due cicli si escludevano a vicenda.
        // Qui la condizione di uscita è una sola e l'attivazione è un effetto collaterale.
        float startedAt = Time.unscaledTime;
        bool activationRequested = false;

        while (!load.isDone)
        {
            _loadingProgress?.RaiseEvent(Mathf.Clamp01(load.progress / 0.9f));

            bool contentReady = load.progress >= 0.9f - 0.001f;
            bool overlayShownLongEnough = Time.unscaledTime - overlayShownAt >= _minimumDisplaySeconds;

            if (!activationRequested && contentReady && overlayShownLongEnough)
            {
                load.allowSceneActivation = true;
                activationRequested = true;
            }

            // Watchdog: uno stallo qui lascerebbe l'overlay a schermo per sempre senza un solo
            // messaggio. Meglio un errore che dice a che punto si è fermato.
            if (Time.unscaledTime - startedAt > LoadWatchdogSeconds)
            {
                Debug.LogError(
                    $"[{nameof(SceneFlowDirector)}] caricamento di '{target.SceneName}' fermo da " +
                    $"{LoadWatchdogSeconds}s (progress={load.progress:F3}, attivazione richiesta=" +
                    $"{activationRequested}). Transizione interrotta.", this);
                return;
            }

            await Awaitable.NextFrameAsync(token);
        }

        _loadingProgress?.RaiseEvent(1f);
        _currentSceneName = target.SceneName;

        // Scena attiva = quella che fornisce illuminazione e skybox. Senza questo la persistente
        // resterebbe attiva e la scena di combattimento verrebbe renderizzata con le sue impostazioni.
        Scene loaded = SceneManager.GetSceneByName(target.SceneName);
        if (loaded.IsValid() && loaded.isLoaded) SceneManager.SetActiveScene(loaded);
    }
}
