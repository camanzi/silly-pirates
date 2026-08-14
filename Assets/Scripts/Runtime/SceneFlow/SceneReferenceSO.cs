using UnityEngine;

/// <summary>
/// Nome di una scena impacchettato in un asset, così che director e bootstrapper si riferiscano a
/// una scena tramite una reference assegnabile in Inspector invece che con stringhe sparse nel
/// codice. Rinominare la scena richiede di aggiornare un solo asset.
///
/// Deliberatamente NON usa gli indici di build: sono posizionali e si rompono in silenzio appena si
/// riordina la lista in Build Settings.
/// </summary>
[CreateAssetMenu(fileName = "SceneRef", menuName = "Scene Flow/Scene Reference")]
public class SceneReferenceSO : ScriptableObject
{
    [Tooltip("Nome della scena come compare in Build Settings, senza percorso né estensione.")]
    [SerializeField] private string _sceneName;

    public string SceneName => _sceneName;

    public bool IsValid => !string.IsNullOrWhiteSpace(_sceneName);

    private void OnValidate()
    {
        if (IsValid) return;

        Debug.LogError(
            $"[{nameof(SceneReferenceSO)}] '{name}' non ha un nome di scena: qualunque transizione " +
            "verso questa reference fallirà a runtime.", this);
    }
}
