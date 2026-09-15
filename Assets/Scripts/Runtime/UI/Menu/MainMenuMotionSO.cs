using System;
using System.Collections.Generic;
using PrimeTween;
using UnityEngine;

/// <summary>
/// Parametri del movimento dell'illustrazione del main menu: l'entrata "a composizione" e il loop
/// di idle dei singoli pezzi del drago.
///
/// L'asset e' STATELESS, come <see cref="LifecycleAnimationSO"/>: qui vivono solo i numeri, mentre i
/// Tween e lo stato di riproduzione stanno in <see cref="MainMenuDrakeAnimator"/>. Cosi' lo stesso
/// asset puo' essere riferito da piu' schermate senza copie per-istanza.
///
/// L'ORDINE di entrata non e' serializzato: lo calcola l'animator dalla distanza orizzontale di ogni
/// pezzo dal centro dello stage. Scriverlo qui a mano significherebbe doverlo rifare a ogni ritocco
/// dell'illustrazione.
/// </summary>
[CreateAssetMenu(fileName = "MainMenuMotion", menuName = "UI/Main Menu Motion")]
public class MainMenuMotionSO : ScriptableObject
{
    /// <summary>Oscillazione in loop di un singolo layer, riferita alla sua posizione a riposo.</summary>
    [Serializable]
    public struct LayerIdle
    {
        [Tooltip("Nome dell'elemento nel documento UXML (es. 'drake-4').")]
        public string elementName;

        [Tooltip("Spostamento massimo in pixel, sui due assi. Zero su un asse = quell'asse non si muove.")]
        public Vector2 amplitude;

        [Tooltip("Secondi per una oscillazione completa, per asse. I due valori vanno tenuti diversi " +
                 "fra loro: se coincidono il pezzo si muove su una diagonale invece che alla deriva.")]
        public Vector2 period;

        [Tooltip("Rotazione massima in gradi. Zero = il pezzo non ruota.")]
        public float rotationAmplitude;

        [Tooltip("Secondi per una oscillazione completa della rotazione.")]
        public float rotationPeriod;

        [Tooltip("Ritardo iniziale: serve a sfasare i layer fra loro, altrimenti respirano all'unisono " +
                 "e l'insieme sembra una singola immagine che trema.")]
        public float startDelay;

        public Ease ease;
    }

    /// <summary>
    /// Tempi del cambio di voce selezionata. La piuma SCORRE da una riga all'altra, mentre le
    /// sottolineature non si spostano: quella vecchia si cancella e quella nuova si scrive.
    /// </summary>
    [Serializable]
    public struct SelectionMotion
    {
        [Tooltip("Durata dello scorrimento della piuma fra due voci.")]
        public float quillDuration;

        public Ease quillEase;

        [Tooltip("Durata della cancellazione della sottolineatura della voce abbandonata (da destra a sinistra).")]
        public float underlineOutDuration;

        public Ease underlineOutEase;

        [Tooltip("Durata della scrittura della sottolineatura della voce scelta (da sinistra a destra).")]
        public float underlineInDuration;

        public Ease underlineInEase;

        [Tooltip("Ritardo della scrittura rispetto alla cancellazione. Tenendolo appena sotto la durata " +
                 "della cancellazione le due si sovrappongono un poco e il passaggio non sembra a scatti.")]
        public float underlineInDelay;

        /// <summary>Valori usati quando manca l'asset, così il menu resta usabile invece di bloccarsi.</summary>
        public static SelectionMotion Default => new()
        {
            quillDuration = 0.25f,
            quillEase = Ease.OutCubic,
            underlineOutDuration = 0.16f,
            underlineOutEase = Ease.InQuad,
            underlineInDuration = 0.24f,
            underlineInEase = Ease.OutQuad,
            underlineInDelay = 0.1f
        };
    }

    /// <summary>
    /// Tempi della title screen: la comparsa del titolo sotto una maschera sfumata che scorre in
    /// orizzontale, e la comparsa/pulsazione del prompt "Press any button".
    ///
    /// Vive qui e non in un SO a parte per lo stesso motivo di <see cref="SelectionMotion"/>: la
    /// title e' una pagina della scena MainMenu, e un secondo asset significherebbe un secondo campo
    /// da tenere agganciato a mano in Editor.
    /// </summary>
    [Serializable]
    public struct TitleMotion
    {
        [Tooltip("Durata della passata della maschera sull'intero titolo.")]
        public float sweepDuration;

        [Tooltip("Ampiezza in pixel della sfumatura del bordo della maschera. Piu' alto = piu' morbido " +
                 "e piu' lettere in dissolvenza insieme; abbassandolo l'effetto si indurisce verso il " +
                 "lettera-per-lettera.")]
        public float sweepFalloff;

        public Ease sweepEase;

        [Tooltip("Pausa fra la fine del titolo e la comparsa del prompt.")]
        public float promptDelay;

        [Tooltip("Durata del fade-in del gruppo del prompt (piuma + testo + sottolineatura).")]
        public float promptFadeDuration;

        [Tooltip("Estremo basso della pulsazione del prompt. L'estremo alto e' sempre 1.")]
        public float promptPulseMinOpacity;

        [Tooltip("Secondi per una SOLA direzione della pulsazione (da semitrasparente a opaco). " +
                 "Il ciclo completo dura il doppio.")]
        public float promptPulseDuration;

        [Tooltip("Durata del fade-out della title screen quando viene congedata.")]
        public float dismissDuration;

        /// <summary>Valori usati quando manca l'asset, così la schermata resta usabile invece di bloccarsi.</summary>
        public static TitleMotion Default => new()
        {
            sweepDuration = 2f,
            sweepFalloff = 300f,
            sweepEase = Ease.InOutSine,
            promptDelay = 0.2f,
            promptFadeDuration = 0.5f,
            promptPulseMinOpacity = 0.5f,
            promptPulseDuration = 2f,
            dismissDuration = 0.35f
        };
    }

    [Header("Entrata")]
    [Tooltip("Distanza fra la partenza di un pezzo e quella del successivo.")]
    [Min(0f)] [SerializeField] private float _stagger = 0.25f;

    [Tooltip("Di quanto un pezzo parte piu' in basso rispetto alla sua posizione finale.")]
    [SerializeField] private float _riseDistance = 160f;

    [Tooltip("Durata della risalita del singolo pezzo.")]
    [Min(0.01f)] [SerializeField] private float _pieceDuration = 0.6f;

    [SerializeField] private Ease _pieceEase = Ease.OutCubic;

    [Header("Entrata delle voci di menu")]
    [Tooltip("Pausa fra la partenza dell'ultimo pezzo del drago e la comparsa delle voci.")]
    [Min(0f)] [SerializeField] private float _menuDelay = 0.15f;

    [Min(0.01f)] [SerializeField] private float _menuDuration = 0.5f;

    [SerializeField] private Ease _menuEase = Ease.OutQuad;

    [Header("Selezione")]
    [SerializeField] private SelectionMotion _selection = SelectionMotion.Default;

    [Header("Title screen")]
    [SerializeField] private TitleMotion _title = TitleMotion.Default;

    [Header("Idle")]
    [SerializeField] private List<LayerIdle> _idle = new();

    public float Stagger => _stagger;
    public float RiseDistance => _riseDistance;
    public float PieceDuration => _pieceDuration;
    public Ease PieceEase => _pieceEase;
    public float MenuDelay => _menuDelay;
    public float MenuDuration => _menuDuration;
    public Ease MenuEase => _menuEase;
    public SelectionMotion Selection => _selection;
    public TitleMotion Title => _title;

    /// <summary>
    /// Scansione lineare invece di un dizionario cachato: i layer sono una manciata e una cache
    /// sarebbe stato mutabile dentro l'asset, che e' esattamente cio' che qui si vuole evitare.
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
