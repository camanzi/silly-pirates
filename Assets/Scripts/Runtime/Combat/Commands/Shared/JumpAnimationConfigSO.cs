using PrimeTween;
using UnityEngine;

/// <summary>
/// Parametri di tuning per un salto in stile squash&amp;stretch (anticipazione, stacco, salita, apice,
/// caduta, atterraggio). Riusabile da qualunque comando che debba far "saltare" il pivot visivo
/// di un personaggio prima/durante/dopo un'azione. Consumata da <see cref="JumpSquashStretchHelper"/>.
/// </summary>
[CreateAssetMenu(fileName = "Jump Animation Config", menuName = "Combat/Animation/Jump Animation Config")]
public class JumpAnimationConfigSO : ScriptableObject
{
    /// <summary>
    /// Fattori di scala coerenti con un pivot billboardato: X e Z restano sempre uguali fra loro
    /// (<see cref="Horizontal"/>) perché il figlio Sprite ruota solo in yaw — con X != Z comparirebbe
    /// uno shear visibile quando la camera non è frontale. Solo Y (<see cref="Vertical"/>) è indipendente,
    /// che è esattamente la lettura classica "schiaccia in verticale, compensa in orizzontale".
    /// </summary>
    [System.Serializable]
    public struct SquashStretchScale
    {
        [Tooltip("Fattore di scala applicato sia a X che a Z (piano orizzontale).")]
        public float Horizontal;
        [Tooltip("Fattore di scala su Y (verticale).")]
        public float Vertical;

        public SquashStretchScale(float horizontal, float vertical)
        {
            Horizontal = horizontal;
            Vertical = vertical;
        }
    }

    [Header("Altezza apice (world space)")]
    [Tooltip("Offset aggiunto alla Y del target per calcolare l'apice, es. per far sporgere il personaggio oltre il bordo del ponte.")]
    [SerializeField] private float _peakHeightOffset = 0.5f;
    [Tooltip("Altezza minima dell'apice sopra la posa di riposo, applicata anche se il target è più basso del caster.")]
    [SerializeField] private float _minPeakHeight = 0.75f;
    [Tooltip("Altezza massima dell'apice, per evitare salti abnormi su target molto in alto.")]
    [SerializeField] private float _maxPeakHeight = 3f;

    [Header("1) Anticipazione (schiacciamento pre-salto)")]
    [SerializeField] private float _anticipationDuration = 0.10f;
    [SerializeField] private SquashStretchScale _anticipationScale = new(1.20f, 0.65f);
    [SerializeField] private Ease _anticipationEase = Ease.OutQuad;

    [Header("2) Stacco (stiramento al lancio)")]
    [SerializeField] private float _launchDuration = 0.07f;
    [SerializeField] private SquashStretchScale _launchScale = new(0.75f, 1.35f);
    [SerializeField] private Ease _launchEase = Ease.OutExpo;

    [Header("3) Salita verso l'apice")]
    [SerializeField] private float _riseDuration = 0.20f;
    [Tooltip("Deve decelerare (OutQuad/OutCubic): è ciò che distingue un salto da una levitazione.")]
    [SerializeField] private Ease _riseEase = Ease.OutQuad;

    [Header("4) Apice (assestamento in sospensione)")]
    [SerializeField] private float _apexSettleDuration = 0.10f;
    [SerializeField] private SquashStretchScale _apexScale = new(0.95f, 1.08f);

    [Header("5) Pausa sospesa opzionale prima dell'azione")]
    [Tooltip("Attesa extra all'apice prima che il comando agisca. 0 = nessuna: il volo del proiettile fa già da hang time.")]
    [SerializeField] private float _hangHoldDuration = 0f;

    [Header("6) Caduta dall'apice")]
    [SerializeField] private float _fallDuration = 0.16f;
    [SerializeField] private SquashStretchScale _fallScale = new(0.90f, 1.15f);
    [Tooltip("Deve accelerare (InQuad/InCubic): è il segnale di peso più forte della sequenza.")]
    [SerializeField] private Ease _fallEase = Ease.InQuad;

    [Header("7) Atterraggio (schiacciamento da impatto)")]
    [SerializeField] private float _landingSquashDuration = 0.08f;
    [SerializeField] private SquashStretchScale _landingScale = new(1.30f, 0.55f);
    [SerializeField] private Ease _landingSquashEase = Ease.OutQuad;

    [Header("8) Recupero elastico verso la posa di riposo")]
    [SerializeField] private float _landingRecoveryDuration = 0.22f;
    [SerializeField] private Ease _landingRecoveryEase = Ease.OutElastic;

    [Header("Splash VFX")]
    [Tooltip("Opzionale: riprodotto allo stacco e all'atterraggio. Se null, lo spawn viene saltato.")]
    [SerializeField] private VFXController _splashVfxPrefab;
    [Tooltip("Canale su cui inoltrare lo splash al VfxDirector. Se null, lo spawn viene saltato.")]
    [SerializeField] private VfxCueEventChannel _vfxChannel;
    [Tooltip("Offset verticale del punto di spawn dello splash rispetto alla posizione del caster (livello dell'acqua).")]
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
    /// Altezza locale dell'apice relativa alla posa di riposo, derivata dalla Y del target e clampata.
    /// Co-locata qui con i parametri che la vincolano, per non duplicare la formula nei chiamanti.
    /// </summary>
    public float ComputeApexHeight(float casterRootWorldY, float targetWorldY)
    {
        float raw = (targetWorldY - casterRootWorldY) + _peakHeightOffset;
        return Mathf.Clamp(raw, _minPeakHeight, _maxPeakHeight);
    }
}
