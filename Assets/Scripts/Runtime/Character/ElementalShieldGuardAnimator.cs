using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

/// <summary>
/// Fa interporre lo scudo davanti al proprio owner mentre un'abilita' ostile e' in esecuzione contro di lui,
/// e lo riporta a riposo quando l'esecuzione finisce.
///
/// Ascolta <see cref="AbilityThreatEventChannel"/> e non l'HealthController: un HealthBehaviorSO vede il
/// colpo solo quando e' gia' arrivato, mentre qui serve reagire PRIMA — fra l'annuncio della minaccia e
/// l'impatto passa tutto il volo del proiettile.
///
/// Tenuto separato da <see cref="ElementalShieldController"/>, che possiede le regole (behavior concessi,
/// elementi, rottura): questo componente e' solo presentazione e puo' essere tolto senza cambiare il gioco.
/// </summary>
[RequireComponent(typeof(ElementalShieldController))]
public class ElementalShieldGuardAnimator : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private AbilityThreatEventChannel _threatChannel;
    [SerializeField] private ShieldGuardAnimationSO _config;

    [Tooltip("Sprite dello scudo. Se non assegnato viene cercato fra i figli.")]
    [SerializeField] private SpriteRenderer _spriteRenderer;

    private ElementalShieldController _shield;
    private HostileCharacter _owner;

    // Posa a riposo catturata una sola volta, prima che qualsiasi tween la sposti.
    private Vector3 _restLocalPosition;
    private Vector3 _restLocalScale;
    private Vector3 _spriteRestLocalPosition;

    // Handle salvati e fermati prima di rilanciare: stesso pattern di DirectionalSpriteController._colorTween.
    // Non si usa Tween.StopAll(transform) perche' ucciderebbe anche i tween di
    // DynamicElementalResistanceCommand, che sta await-ando i propri.
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

    // Sola sottoscrizione, nessun cue alzato: in OnEnable i director possono non aver ancora fatto Awake.
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
    /// Vero se la minaccia riguarda l'owner o una qualsiasi delle sue parti — lo scudo si alza sia quando si
    /// mira al corpo sia quando si mira allo scudo stesso, che e' un bersaglio cliccabile a se'.
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
    /// Elemento in arrivo. Diverso da quello attivo (o <see cref="DamageType.None"/>) = para, il danno verra'
    /// azzerato dall'immunita'. Uguale = incassa, e' il colpo che sta rompendo lo scudo.
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

    // Lampeggio col colore dell'elemento in arrivo e ritorno (Yoyo) al tint dell'elemento attivo, che e'
    // gia' quello dello sprite in questo istante.
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

    // Lo shake va sul figlio sprite, non sulla radice: la radice la sta gia' tweenando la salita in guardia
    // e i due si contenderebbero lo stesso localPosition.
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

        // Lo sprite torna subito: lo shake e' gia' finito e il colore deve tornare al tint dell'elemento
        // attivo, che nel frattempo puo' essere cambiato.
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

    // Tween.Stop() di PrimeTween e' un kill, non un rewind: uno shake fermato a meta' oscillazione lascia il
    // pivot sfasato, quindi la posa va riscritta a mano.
    private void ResetSpriteToRest()
    {
        if (_spriteRenderer == null) return;

        _spriteRenderer.transform.localPosition = _spriteRestLocalPosition;
        if (_shield != null && _shield.TryGetTint(_shield.ActiveElement, out Color tint))
            _spriteRenderer.color = tint;
    }
}
