using PrimeTween;
using UnityEngine;

/// <summary>
/// Parametri della reazione di guardia dello scudo elementale. Stateless e condivisibile fra piu' scudi,
/// come le LifecycleAnimationSO: tutto lo stato mutabile (pose a riposo, handle dei tween) vive nel
/// <see cref="ElementalShieldGuardAnimator"/>.
/// </summary>
[CreateAssetMenu(fileName = "Shield Guard Animation", menuName = "Character/Shield Guard Animation")]
public class ShieldGuardAnimationSO : ScriptableObject
{
    [Header("Posa di guardia")]
    [Tooltip("Posizione locale che lo scudo assume da alzato. Coordinate assolute nello spazio del padre, non uno scostamento dal riposo: si legge direttamente sul Transform in scena.")]
    [SerializeField] private Vector3 _guardLocalPosition = new(0f, 0f, -0.25f);
    [SerializeField] private float _guardScaleMultiplier = 1.25f;

    [Header("Salita / discesa")]
    [SerializeField] private float _raiseDuration = 0.22f;
    [SerializeField] private Ease _raiseEase = Ease.OutBack;
    [SerializeField] private float _lowerDuration = 0.28f;
    [SerializeField] private Ease _lowerEase = Ease.OutQuad;

    [Header("Para (elemento diverso da quello attivo: danno azzerato)")]
    [Tooltip("Lampeggio col colore dell'elemento in arrivo, poi ritorno al tint dell'elemento attivo.")]
    [SerializeField] private float _blockFlashDuration = 0.12f;
    [SerializeField] private int _blockFlashCycles = 2;
    [Tooltip("Colore usato quando l'abilita' e' ostile ma senza elemento (rete, maledizione).")]
    [SerializeField] private Color _colorlessFlashTint = new(1f, 1f, 1f, 1f);

    [Header("Incassa (elemento giusto: lo scudo si sta rompendo)")]
    [SerializeField] private Vector3 _absorbShakeStrength = new(0.12f, 0.04f, 0f);
    [SerializeField] private float _absorbShakeDuration = 0.35f;
    [SerializeField] private int _absorbShakeCycles = 6;

    [Header("VFX (opzionale)")]
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

    /// <summary>Alza il VFX di para, se configurato. Doppio null-check come ovunque nel progetto: entrambi i campi sono opzionali.</summary>
    public void RaiseBlockVfx(Vector3 position)
    {
        if (_blockVfx == null || _vfxChannel == null) return;
        _vfxChannel.RaiseEvent(VfxCue.At(_blockVfx, position));
    }
}
