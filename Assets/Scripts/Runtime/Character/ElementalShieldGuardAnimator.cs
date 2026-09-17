using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

/// <summary>
/// Interposes the shield in front of its owner while a hostile ability is executing against them, and
/// brings it back to rest when that execution ends.
///
/// It listens to <see cref="AbilityThreatEventChannel"/> and not to the HealthController: a
/// HealthBehaviorSO only sees the hit once it has already landed, whereas what is needed here is to react
/// BEFORE — the whole flight of the projectile sits between the threat being announced and the impact.
///
/// Kept separate from <see cref="ElementalShieldController"/>, which owns the rules (granted behaviors,
/// elements, breaking): this component is presentation only and can be removed without changing the game.
/// </summary>
[RequireComponent(typeof(ElementalShieldController))]
public class ElementalShieldGuardAnimator : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private AbilityThreatEventChannel _threatChannel;
    [SerializeField] private ShieldGuardAnimationSO _config;

    [Tooltip("The shield's sprite. When unassigned it is looked up among the children.")]
    [SerializeField] private SpriteRenderer _spriteRenderer;

    private ElementalShieldController _shield;
    private HostileCharacter _owner;

    // The rest pose, captured exactly once, before any tween can move it.
    private Vector3 _restLocalPosition;
    private Vector3 _restLocalScale;
    private Vector3 _spriteRestLocalPosition;

    // Handles stored and stopped before relaunching: the same pattern as
    // DirectionalSpriteController._colorTween. Tween.StopAll(transform) is deliberately not used, because
    // it would also kill DynamicElementalResistanceCommand's tweens, which it is awaiting.
    private Tween _moveTween;
    private Tween _scaleTween;
    private Tween _colorTween;
    private Tween _shakeTween;

    private bool _isGuarding;

    private void Awake()
    {
        _shield = GetComponent<ElementalShieldController>();
        _owner  = GetComponentInParent<HostileCharacter>();

        if (_spriteRenderer == null) _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        _restLocalPosition = transform.localPosition;
        _restLocalScale    = transform.localScale;
        if (_spriteRenderer != null) _spriteRestLocalPosition = _spriteRenderer.transform.localPosition;
    }

    // Subscription only, no cue raised: in OnEnable the directors may not have run Awake yet.
    private void OnEnable()
    {
        if (_threatChannel != null) _threatChannel.OnEventRaised += HandleThreat;
    }

    private void OnDisable()
    {
        if (_threatChannel != null) _threatChannel.OnEventRaised -= HandleThreat;

        StopTweens();
        ResetToRest();
        _isGuarding = false;
    }

    private void HandleThreat(AbilityThreatCue cue)
    {
        if (!cue.Active)
        {
            LowerGuard();
            return;
        }

        if (_config == null) return;
        if (cue.Intent != AbilityIntent.Offensive) return;
        if (_shield == null || !_shield.IsActive) return;
        if (!TargetsOwner(cue.Targets)) return;

        RaiseGuard(cue.Element);
    }

    /// <summary>
    /// True when the threat concerns the owner or any of its parts — the shield goes up both when the body
    /// is aimed at and when the shield itself is, since that is a clickable target in its own right.
    /// </summary>
    private bool TargetsOwner(IReadOnlyList<ITargettable> targets)
    {
        if (targets == null || _owner == null) return false;

        Transform ownerTransform = _owner.transform;
        for (int i = 0; i < targets.Count; i++)
        {
            ITargettable target = targets[i];
            if (target == null) continue;
            if (ReferenceEquals(target, _owner)) return true;

            Transform targetTransform = (target as Component)?.transform;
            if (targetTransform != null && targetTransform.IsChildOf(ownerTransform)) return true;
        }

        return false;
    }

    /// <param name="incoming">
    /// The incoming element. Different from the active one (or <see cref="DamageType.None"/>) = a block,
    /// the damage will be halved by the resistance. The same = it takes and absorbs the hit, the one the
    /// body turns into healing.
    /// </param>
    private void RaiseGuard(DamageType incoming)
    {
        StopTweens();

        bool absorbs = incoming != DamageType.None && incoming == _shield.ActiveElement;

        _moveTween  = Tween.LocalPosition(transform, _config.GuardLocalPosition,
                                          _config.RaiseDuration, _config.RaiseEase);
        _scaleTween = Tween.Scale(transform, _restLocalScale * _config.GuardScaleMultiplier,
                                  _config.RaiseDuration, _config.RaiseEase);

        if (absorbs) PlayAbsorb();
        else         PlayBlock(incoming);

        _isGuarding = true;
    }

    // Flash with the incoming element's colour and return (Yoyo) to the active element's tint, which is
    // already the sprite's colour at this instant.
    private void PlayBlock(DamageType incoming)
    {
        _config.RaiseBlockVfx(transform.position);

        if (_spriteRenderer == null) return;

        Color flash = _shield.TryGetTint(incoming, out Color elementTint)
            ? elementTint
            : _config.ColorlessFlashTint;

        _colorTween = Tween.Color(_spriteRenderer, flash, _config.BlockFlashDuration,
                                  Ease.InOutQuad, _config.BlockFlashCycles, CycleMode.Yoyo);
    }

    // The shake goes on the sprite child, not on the root: the root is already being tweened by the raise
    // into guard, and the two would fight over the same localPosition.
    private void PlayAbsorb()
    {
        if (_spriteRenderer == null) return;

        _shakeTween = Tween.ShakeLocalPosition(_spriteRenderer.transform,
                                               strength: _config.AbsorbShakeStrength,
                                               duration: _config.AbsorbShakeDuration,
                                               cycles: _config.AbsorbShakeCycles);
    }

    private void LowerGuard()
    {
        if (!_isGuarding) return;
        _isGuarding = false;

        StopTweens();

        if (_config == null) { ResetToRest(); return; }

        _moveTween  = Tween.LocalPosition(transform, _restLocalPosition, _config.LowerDuration, _config.LowerEase);
        _scaleTween = Tween.Scale(transform, _restLocalScale, _config.LowerDuration, _config.LowerEase);

        // The sprite snaps back at once: the shake is over already and the colour has to return to the
        // active element's tint, which may have changed in the meantime.
        ResetSpriteToRest();
    }

    private void StopTweens()
    {
        if (_moveTween.isAlive)  _moveTween.Stop();
        if (_scaleTween.isAlive) _scaleTween.Stop();
        if (_colorTween.isAlive) _colorTween.Stop();
        if (_shakeTween.isAlive) _shakeTween.Stop();
    }

    private void ResetToRest()
    {
        transform.localPosition = _restLocalPosition;
        transform.localScale    = _restLocalScale;
        ResetSpriteToRest();
    }

    // PrimeTween's Tween.Stop() is a kill, not a rewind: a shake stopped mid-oscillation leaves the pivot
    // offset, so the pose has to be rewritten by hand.
    private void ResetSpriteToRest()
    {
        if (_spriteRenderer == null) return;

        _spriteRenderer.transform.localPosition = _spriteRestLocalPosition;
        if (_shield != null && _shield.TryGetTint(_shield.ActiveElement, out Color tint))
            _spriteRenderer.color = tint;
    }
}
