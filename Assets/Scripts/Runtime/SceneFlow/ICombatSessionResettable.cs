/// <summary>
/// The minimal contract for every ScriptableObject carrying non-serialized runtime state meant to
/// survive between one load of the asset and the next (private fields, static HashSets/Dictionaries,
/// counters). In an additive multi-scene architecture these fields are NOT cleared by unloading the
/// combat scene: a ScriptableObject's OnEnable is an asset-loaded-into-memory hook, not a scene-loaded
/// one, so it fires exactly once per Play/build session.
/// CombatSessionSO invokes ResetForNewCombat() on every registered implementor, between unloading the
/// old scene and loading the new one.
/// </summary>
public interface ICombatSessionResettable
{
    void ResetForNewCombat();
}
