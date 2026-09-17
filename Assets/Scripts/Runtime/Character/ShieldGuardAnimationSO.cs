using PrimeTween;
using UnityEngine;

/// <summary>
/// The parameters of the elemental shield's guard reaction. Stateless and shareable between several
/// shields, like the LifecycleAnimationSO assets: all mutable state (rest poses, tween handles) lives on
/// the <see cref="ElementalShieldGuardAnimator"/>.
/// </summary>
[CreateAssetMenu(fileName = "Shield Guard Animation", menuName = "Character/Shield Guard Animation")]
public class ShieldGuardAnimationSO : ScriptableObject
{
    [Header("Guard pose")]
    [Tooltip("The local position the shield takes when raised. Absolute coordinates in the parent's space, not an offset from rest: it can be read straight off the Transform in the scene.")]
    [SerializeField] private Vector3 _guardLocalPosition = new(0f, 0f, -0.25f);
    [SerializeField] private float _guardScaleMultiplier = 1.25f;

    [Header("Raise / lower")]
    [SerializeField] private float _raiseDuration = 0.22f;
    [SerializeField] private Ease _raiseEase = Ease.OutBack;
    [SerializeField] private float _lowerDuration = 0.28f;
    [SerializeField] private Ease _lowerEase = Ease.OutQuad;

    [Header("Block (element other than the active one: damage nullified)")]
    [Tooltip("Flash with the incoming element's colour, then return to the active element's tint.")]
    [SerializeField] private float _blockFlashDuration = 0.12f;
    [SerializeField] private int _blockFlashCycles = 2;
    [Tooltip("The colour used when the ability is hostile but elementless (net, curse).")]
    [SerializeField] private Color _colorlessFlashTint = new(1f, 1f, 1f, 1f);

    [Header("Take the hit (the right element: the shield is breaking)")]
    [SerializeField] private Vector3 _absorbShakeStrength = new(0.12f, 0.04f, 0f);
    [SerializeField] private float _absorbShakeDuration = 0.35f;
    [SerializeField] private int _absorbShakeCycles = 6;

    [Header("VFX (optional)")]
    [SerializeField] private VFXController _blockVfx;
    [SerializeField] private VfxCueEventChannel _vfxChannel;

    public Vector3 GuardLocalPosition => _guardLocalPosition;
    public float GuardScaleMultiplier => _guardScaleMultiplier;
    public float RaiseDuration => _raiseDuration;
    public Ease RaiseEase => _raiseEase;
    public float LowerDuration => _lowerDuration;
    public Ease LowerEase => _lowerEase;
    public float BlockFlashDuration => _blockFlashDuration;
    public int BlockFlashCycles => _blockFlashCycles;
    public Color ColorlessFlashTint => _colorlessFlashTint;
    public Vector3 AbsorbShakeStrength => _absorbShakeStrength;
    public float AbsorbShakeDuration => _absorbShakeDuration;
    public int AbsorbShakeCycles => _absorbShakeCycles;

    /// <summary>Raises the block VFX, when configured. A double null-check as everywhere else in the project: both fields are optional.</summary>
    public void RaiseBlockVfx(Vector3 position)
    {
        if (_blockVfx == null || _vfxChannel == null) return;
        _vfxChannel.RaiseEvent(VfxCue.At(_blockVfx, position));
    }
}
