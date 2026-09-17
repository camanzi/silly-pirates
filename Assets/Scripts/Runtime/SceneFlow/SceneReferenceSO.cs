using UnityEngine;

/// <summary>
/// A scene's name packaged into an asset, so that directors and bootstrappers refer to a scene through
/// an Inspector-assignable reference rather than through strings scattered across the code. Renaming the
/// scene means updating a single asset.
///
/// It deliberately does NOT use build indices: those are positional and break silently the moment the
/// list in Build Settings is reordered.
/// </summary>
[CreateAssetMenu(fileName = "SceneRef", menuName = "Scene Flow/Scene Reference")]
public class SceneReferenceSO : ScriptableObject
{
    [Tooltip("The scene's name as it appears in Build Settings, with no path and no extension.")]
    [SerializeField] private string _sceneName;

    public string SceneName => _sceneName;

    public bool IsValid => !string.IsNullOrWhiteSpace(_sceneName);

    private void OnValidate()
    {
        if (IsValid) return;

        Debug.LogError(
            $"[{nameof(SceneReferenceSO)}] '{name}' has no scene name: any transition towards this " +
            "reference will fail at runtime.", this);
    }
}
