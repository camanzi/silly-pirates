using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

/// <summary>
/// Parameters for the main menu illustration's motion: the "assembling" entrance and the idle loop
/// of the drake's individual pieces.
///
/// The asset is STATELESS, like <see cref="LifecycleAnimationSO"/>: only numbers live here, while
/// the Tweens and playback state live in <see cref="MainMenuDrakeAnimator"/>. This way the same
/// asset can be referenced by multiple screens with no per-instance copies.
///
/// The entrance ORDER is not serialized: the animator computes it from each piece's horizontal
/// distance from the stage centre. Writing it here by hand would mean redoing it on every retouch
/// of the illustration.
///
/// The title screen's and the menu selection's own timings used to live here too (as TitleMotion
/// and SelectionMotion). They have moved onto the components that actually own that motion
/// (SweepRevealLabel, SelectionMarker, MenuButton, PulsingGroup) as UxmlAttributes, so they can be
/// tuned in UI Builder next to the element they animate instead of in a separate asset.
/// </summary>
[CreateAssetMenu(fileName = "MainMenuMotion", menuName = "UI/Main Menu Motion")]
public class MainMenuMotionSO : ScriptableObject
{
    /// <summary>Looping oscillation of a single layer, relative to its resting position.</summary>
    [Serializable]
    public struct LayerIdle
    {
        [Tooltip("Name of the element in the UXML document (e.g. 'drake-4'). Must match element.name exactly.")]
        public string elementName;

        [Tooltip("Maximum displacement in pixels, on both axes. Zero on an axis = that axis does not move.")]
        public Vector2 amplitude;

        [Tooltip("Seconds for one full oscillation, per axis. The two values should be kept different " +
                 "from each other: if they match, the piece drifts along a diagonal instead of adrift.")]
        public Vector2 period;

        [Tooltip("Maximum rotation in degrees. Zero = the piece does not rotate.")]
        public float rotationAmplitude;

        [Tooltip("Seconds for one full oscillation of the rotation.")]
        public float rotationPeriod;

        [Tooltip("Initial delay: staggers layers against each other, otherwise they breathe in unison " +
                 "and the whole thing reads as a single trembling image.")]
        public float startDelay;

        public Ease ease;
    }

    [Header("Entrance")]
    [Tooltip("Gap between one piece starting and the next.")]
    [Min(0f)] [SerializeField] private float _stagger = 0.25f;

    [Tooltip("How far below its final position a piece starts.")]
    [SerializeField] private float _riseDistance = 160f;

    [Tooltip("Duration of a single piece's rise.")]
    [Min(0.01f)] [SerializeField] private float _pieceDuration = 0.6f;

    [SerializeField] private Ease _pieceEase = Ease.OutCubic;

    [Header("Menu entries entrance")]
    [Tooltip("Pause between the last piece of the drake starting and the menu entries appearing.")]
    [Min(0f)] [SerializeField] private float _menuDelay = 0.15f;

    [Min(0.01f)] [SerializeField] private float _menuDuration = 0.5f;

    [SerializeField] private Ease _menuEase = Ease.OutQuad;

    [Header("Idle")]
    [SerializeField] private List<LayerIdle> _idle = new();

    public float Stagger => _stagger;
    public float RiseDistance => _riseDistance;
    public float PieceDuration => _pieceDuration;
    public Ease PieceEase => _pieceEase;
    public float MenuDelay => _menuDelay;
    public float MenuDuration => _menuDuration;
    public Ease MenuEase => _menuEase;

    /// <summary>
    /// Linear scan instead of a cached dictionary: the layers are a handful, and a cache would be
    /// mutable state inside the asset, which is exactly what this design wants to avoid.
    /// </summary>
    public bool TryGetIdle(string elementName, out LayerIdle idle)
    {
        for (int i = 0; i < _idle.Count; i++)
        {
            if (_idle[i].elementName != elementName) continue;

            idle = _idle[i];
            return true;
        }

        idle = default;
        return false;
    }
}
