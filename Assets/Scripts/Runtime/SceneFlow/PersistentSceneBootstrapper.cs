using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// An Editor convenience: it lets you hit Play directly on a content scene (MainMenu or combat) by
/// loading the persistent scene if it is not there already, so that the camera, audio and VFX exist
/// regardless.
///
/// The body is wrapped in UNITY_EDITOR on purpose: in builds the only entry point is the persistent
/// scene, and there is no second start-up path to keep alive. A bootstrap active in builds too would be
/// a road nobody ever travels, and would therefore rot unnoticed.
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
                $"[{nameof(PersistentSceneBootstrapper)}] persistent scene not assigned: hitting Play " +
                "from this scene will leave the camera, audio and VFX missing.", this);
            return;
        }

        if (SceneManager.GetSceneByName(_persistentScene.SceneName).isLoaded) return;

        // Synchronous rather than additive-async: the load completes by the end of the frame anyway, so
        // the persistent systems are available from the next frame onwards. Consumers resolving through
        // RuntimeAnchorSO already handle the wait (pull-then-subscribe), the others have to null-guard.
        SceneManager.LoadScene(_persistentScene.SceneName, LoadSceneMode.Additive);
#endif
    }
}
