using UnityEngine;

/// <summary>
/// Implemented by anything that wants to declare its own firing origin explicitly, bypassing the
/// automatic <see cref="MuzzleAnchor"/> lookup in the hierarchy.
/// It may return null: the fallback chain is <see cref="MuzzleUtils"/>'s responsibility.
/// </summary>
public interface IMuzzleOwner
{
    Transform Muzzle { get; }
}
