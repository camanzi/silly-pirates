using UnityEngine;

/// <summary>
/// Marks the point projectiles, muzzle VFX and SFX originate from.
/// It goes on a child GameObject placed at the weapon's muzzle;
/// its Z axis (forward) defines the VFX's direction.
/// </summary>
[DisallowMultipleComponent]
public class MuzzleAnchor : MonoBehaviour { }
