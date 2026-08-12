using UnityEngine;

/// <summary>
/// Implementato da chi vuole dichiarare esplicitamente la propria origine di tiro,
/// scavalcando la ricerca automatica del <see cref="MuzzleAnchor"/> in hierarchy.
/// Puo' restituire null: la catena di fallback e' responsabilita' di <see cref="MuzzleUtils"/>.
/// </summary>
public interface IMuzzleOwner
{
    Transform Muzzle { get; }
}
