using System;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// The orchestrator of transitions between the content scenes (MainMenu and combat), which are loaded
/// additively on top of the persistent scene. It lives in the persistent one and is never unloaded.
///
/// The order of the three operations is non-negotiable: UNLOAD the old scene, then RESET the session
/// state, then LOAD the new one. Resetting after the load would wipe the registrations the incoming
/// scene's OnEnable calls have just made (grid occupancy, agents in the turn queue), which is precisely
/// the bug the reset is meant to prevent.
/// </summary>
public class SceneFlowDirector : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private SceneReferenceSO _mainMenuScene;
    [SerializeField] private SceneReferenceSO _combatScene;

    [Header("Session")]
    [Tooltip("The list of SOs with runtime state cleared between one unload and the next load.")]
    [SerializeField] private CombatSessionSO _combatSession;

    [Header("Channels")]
    [SerializeField] private VoidEventChannel _startCombatRequested;
    [SerializeField] private VoidEventChannel _returnToMenuRequested;
    [SerializeField] private FloatEventChannel _loadingProgress;
    [SerializeField] private BoolEventChannel _loadingVisible;

    [Header("Config")]
    [Tooltip("Minimum time the overlay stays up. The scenes here are small, and without this the bar " +
             "would flash past before the percentage could be read.")]
    [Min(0f)] [SerializeField] private float _minimumDisplaySeconds = 0.75f;

    private const float LoadWatchdogSeconds = 30f;

    // The content scene currently loaded. The persistent one never appears here: it is never unloaded.
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
        // If a content scene is already loaded, we were started by the PersistentSceneBootstrapper
        // (hitting Play directly on a gameplay scene in the Editor): it is adopted instead of loading the
        // menu on top, which would leave two content scenes alive at once.
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
            Debug.LogError($"[{nameof(SceneFlowDirector)}] destination scene not assigned, or nameless.", this);
            return;
        }

        // A second click on the button while the transition is running would unload a scene halfway
        // through loading: it is ignored rather than queued.
        if (_isTransitioning) return;
        _isTransitioning = true;

        try
        {
            _loadingVisible?.RaiseEvent(true);
            _loadingProgress?.RaiseEvent(0f);

            // unscaledTime: a transition must not depend on the game's timeScale.
            float overlayShownAt = Time.unscaledTime;

            // At least one frame is always yielded before touching the scenes, so this method can never
            // complete synchronously and the overlay gets a frame to draw itself.
            await Awaitable.NextFrameAsync(token);

            await UnloadCurrentSceneAsync(token);

            _combatSession?.ResetForNewCombat();

            await LoadSceneAsync(target, overlayShownAt, token);

            _loadingVisible?.RaiseEvent(false);
        }
        catch (OperationCanceledException)
        {
            // The director was destroyed mid-transition (leaving Play Mode): nothing to report.
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
                $"[{nameof(SceneFlowDirector)}] '{target.SceneName}' cannot be loaded: is it missing " +
                "from the list in Build Settings?", this);
            return;
        }

        // With activation held back, Unity caps progress at 0.9: it is renormalized so the percentage
        // actually reaches 100 instead of stopping at 90.
        load.allowSceneActivation = false;

        // A single loop driven by isDone. The previous version waited first on "progress >= 0.9" and
        // then on "isDone" in two separate loops, and would hang: the bare comparison against 0.9 is not
        // reliable (the value can settle just below it) and the two loops excluded each other. Here there
        // is a single exit condition and activation is a side effect.
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

            // Watchdog: a stall here would leave the overlay on screen forever without a single message.
            // Better an error that says where it got stuck.
            if (Time.unscaledTime - startedAt > LoadWatchdogSeconds)
            {
                Debug.LogError(
                    $"[{nameof(SceneFlowDirector)}] loading of '{target.SceneName}' stuck for " +
                    $"{LoadWatchdogSeconds}s (progress={load.progress:F3}, activation requested=" +
                    $"{activationRequested}). Transition aborted.", this);
                return;
            }

            await Awaitable.NextFrameAsync(token);
        }

        _loadingProgress?.RaiseEvent(1f);
        _currentSceneName = target.SceneName;

        // The active scene is the one providing lighting and the skybox. Without this the persistent one
        // would stay active and the combat scene would be rendered with its settings.
        Scene loaded = SceneManager.GetSceneByName(target.SceneName);
        if (loaded.IsValid() && loaded.isLoaded) SceneManager.SetActiveScene(loaded);
    }
}
