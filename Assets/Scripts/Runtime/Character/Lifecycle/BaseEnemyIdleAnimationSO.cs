using PrimeTween;
using UnityEngine;

/// <summary>
/// Idle di base dei nemici: una lenta oscillazione sull'asse Y, sola posizione — mai la scala, che il loop
/// non deve appropriarsi (la stanno già usando <see cref="JumpSquashStretchHelper"/> e i comandi legacy di
/// squash-stretch). Concreta di riferimento da cui copiare per i prossimi loop (es. un futuro PostDeath).
/// </summary>
[CreateAssetMenu(menuName = "Combat/Lifecycle Animations/Base Enemy Idle")]
public class BaseEnemyIdleAnimationSO : LoopingLifecycleAnimationSO
{
    [Header("Base Enemy Idle")]
    [SerializeField] private float _amplitude = 0.12f;
    [SerializeField] private float _duration = 1.6f;
    [SerializeField] private Ease _ease = Ease.InOutSine;

    [Tooltip("Sfasamento iniziale casuale, in secondi: senza, corpo e satelliti di un nemico composito " +
             "(es. BigEvilCube) ondeggerebbero come un blocco unico invece che come parti indipendenti.")]
    [SerializeField] private float _maxStartOffset = 0.8f;

    public override void PrepareTarget(in LifecycleAnimationTarget target)
    {
        if (target.Pivot == null) return;

        // Solo la posizione: l'avvio di un loop non deve toccare una scala che un'altra animazione
        // potrebbe aver lasciato a metà, non è compito suo ripristinarla.
        target.Pivot.localPosition = target.RestLocalPosition;
    }

    public override Tween PlayTarget(in LifecycleAnimationTarget target)
    {
        if (target.Pivot == null) return default;

        // Random.Range letto ORA, al momento della chiamata, e non memorizzato: la SO resta stateless e
        // condivisa fra prefab, lo sfasamento è per-bersaglio-per-avvio, non per-asset.
        return Tween.LocalPositionY(
            target.Pivot, target.RestLocalPosition.y + _amplitude, _duration, _ease,
            cycles: -1, cycleMode: CycleMode.Yoyo,
            startDelay: Random.Range(0f, _maxStartOffset));
    }
}
