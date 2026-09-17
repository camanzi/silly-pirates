using PrimeTween;
using UnityEngine;

/// <summary>
/// Tuning parameters for a squash&amp;stretch jump (anticipation, launch, rise, apex, fall, landing).
/// Reusable by any command that needs to make a character's visual pivot "jump" before, during or after
/// an action. Consumed by <see cref="JumpSquashStretchHelper"/>.
/// </summary>
[CreateAssetMenu(fileName = "Jump Animation Config", menuName = "Combat/Animation/Jump Animation Config")]
public class JumpAnimationConfigSO : ScriptableObject
{
    /// <summary>
    /// Scale factors consistent with a billboarded pivot: X and Z always stay equal to each other
    /// (<see cref="Horizontal"/>) because the Sprite child only rotates in yaw — with X != Z a visible
    /// shear would appear whenever the camera is not head-on. Only Y (<see cref="Vertical"/>) is
    /// independent, which is exactly the classic "squash vertically, compensate horizontally" reading.
    /// </summary>
    [System.Serializable]
    public struct SquashStretchScale
    {
        [Tooltip("Scale factor applied to both X and Z (the horizontal plane).")]
        public float Horizontal;
        [Tooltip("Scale factor on Y (vertical).")]
        public float Vertical;

        public SquashStretchScale(float horizontal, float vertical)
        {
            Horizontal = horizontal;
            Vertical = vertical;
        }
    }

    [Header("Apex height (world space)")]
    [Tooltip("Offset added to the target's Y to compute the apex, e.g. to make the character rise past the edge of the deck.")]
    [SerializeField] private float _peakHeightOffset = 0.5f;
    [Tooltip("Minimum apex height above the rest pose, applied even when the target sits lower than the caster.")]
    [SerializeField] private float _minPeakHeight = 0.75f;
    [Tooltip("Maximum apex height, to avoid absurd leaps at targets that sit very high up.")]
    [SerializeField] private float _maxPeakHeight = 3f;

    [Header("1) Anticipation (pre-jump squash)")]
    [SerializeField] private float _anticipationDuration = 0.10f;
    [SerializeField] private SquashStretchScale _anticipationScale = new(1.20f, 0.65f);
    [SerializeField] private Ease _anticipationEase = Ease.OutQuad;

    [Header("2) Launch (stretch on take-off)")]
    [SerializeField] private float _launchDuration = 0.07f;
    [SerializeField] private SquashStretchScale _launchScale = new(0.75f, 1.35f);
    [SerializeField] private Ease _launchEase = Ease.OutExpo;

    [Header("3) Rise towards the apex")]
    [SerializeField] private float _riseDuration = 0.20f;
    [Tooltip("Must decelerate (OutQuad/OutCubic): that is what tells a jump apart from levitation.")]
    [SerializeField] private Ease _riseEase = Ease.OutQuad;

    [Header("4) Apex (settling while suspended)")]
    [SerializeField] private float _apexSettleDuration = 0.10f;
    [SerializeField] private SquashStretchScale _apexScale = new(0.95f, 1.08f);

    [Header("5) Optional hang before the action")]
    [Tooltip("Extra wait at the apex before the command acts. 0 = none: the projectile's flight already serves as hang time.")]
    [SerializeField] private float _hangHoldDuration = 0f;

    [Header("6) Fall from the apex")]
    [SerializeField] private float _fallDuration = 0.16f;
    [SerializeField] private SquashStretchScale _fallScale = new(0.90f, 1.15f);
    [Tooltip("Must accelerate (InQuad/InCubic): it is the strongest signal of weight in the whole sequence.")]
    [SerializeField] private Ease _fallEase = Ease.InQuad;

    [Header("7) Landing (squash from the impact)")]
    [SerializeField] private float _landingSquashDuration = 0.08f;
    [SerializeField] private SquashStretchScale _landingScale = new(1.30f, 0.55f);
    [SerializeField] private Ease _landingSquashEase = Ease.OutQuad;

    [Header("8) Elastic recovery back to the rest pose")]
    [SerializeField] private float _landingRecoveryDuration = 0.22f;
    [SerializeField] private Ease _landingRecoveryEase = Ease.OutElastic;

    [Header("Splash VFX")]
    [Tooltip("Optional: played on take-off and on landing. When null, the spawn is skipped.")]
    [SerializeField] private VFXController _splashVfxPrefab;
    [Tooltip("The channel the splash is forwarded to the VfxDirector on. When null, the spawn is skipped.")]
    [SerializeField] private VfxCueEventChannel _vfxChannel;
    [Tooltip("Vertical offset of the splash spawn point relative to the caster's position (the water level).")]
    [SerializeField] private float _splashYOffset = 0.05f;

    public float AnticipationDuration => _anticipationDuration;
    public SquashStretchScale AnticipationScale => _anticipationScale;
    public Ease AnticipationEase => _anticipationEase;

    public float LaunchDuration => _launchDuration;
    public SquashStretchScale LaunchScale => _launchScale;
    public Ease LaunchEase => _launchEase;

    public float RiseDuration => _riseDuration;
    public Ease RiseEase => _riseEase;

    public float ApexSettleDuration => _apexSettleDuration;
    public SquashStretchScale ApexScale => _apexScale;

    public float HangHoldDuration => _hangHoldDuration;

    public float FallDuration => _fallDuration;
    public SquashStretchScale FallScale => _fallScale;
    public Ease FallEase => _fallEase;

    public float LandingSquashDuration => _landingSquashDuration;
    public SquashStretchScale LandingScale => _landingScale;
    public Ease LandingSquashEase => _landingSquashEase;

    public float LandingRecoveryDuration => _landingRecoveryDuration;
    public Ease LandingRecoveryEase => _landingRecoveryEase;

    public VFXController SplashVfxPrefab => _splashVfxPrefab;
    public VfxCueEventChannel VfxChannel => _vfxChannel;
    public float SplashYOffset => _splashYOffset;

    /// <summary>
    /// The apex's local height relative to the rest pose, derived from the target's Y and clamped.
    /// Co-located here with the parameters that constrain it, so the formula is not duplicated in callers.
    /// </summary>
    public float ComputeApexHeight(float casterRootWorldY, float targetWorldY)
    {
        float raw = (targetWorldY - casterRootWorldY) + _peakHeightOffset;
        return Mathf.Clamp(raw, _minPeakHeight, _maxPeakHeight);
    }
}
