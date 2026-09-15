using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Overlay di caricamento. Vive nella scena persistente, su un PanelSettings con sortingOrder più
/// alto di quello della HUD: deve restare visibile mentre la scena di contenuto sotto viene
/// scaricata e ricaricata.
///
/// Non conosce SceneFlowDirector: riceve la visibilità da un canale, quindi non ha riferimenti a
/// oggetti che possono sparire con una scena.
///
/// La schermata non mostra alcun avanzamento, per scelta di design: le scene qui sono piccole e una
/// percentuale che salta da 0 a 100 dice meno di niente. Al suo posto due animazioni che servono
/// solo a non lasciare un'immagine ferma — i puntini e l'oscillazione della piuma. Il canale del
/// progresso continua a esistere ed essere alzato da SceneFlowDirector, semplicemente non ha più
/// nessuno in ascolto.
/// </summary>
public class LoadingScreenController : MonoBehaviour
{
    /// <summary>
    /// Le quattro parole sono precostruite e non composte a runtime: il ciclo gira per tutta la
    /// durata del caricamento, e concatenare un suffisso ogni volta allocherebbe per nulla.
    /// </summary>
    private static readonly string[] DotStates = { "Loading", "Loading.", "Loading..", "Loading..." };

    [SerializeField] private UIDocument _document;

    [Header("Canali")]
    [SerializeField] private BoolEventChannel _visibilityChannel;

    [Header("Config")]
    [SerializeField] private float _fadeDuration = 0.25f;

    [Tooltip("Millisecondi fra un puntino e il successivo. A 1000 il ciclo completo 0 -> 3 -> 0 dura " +
             "i 4 secondi di progetto.")]
    [Min(1)] [SerializeField] private int _dotIntervalMs = 1000;

    [Tooltip("Spostamento verticale massimo della piuma, in pixel.")]
    [SerializeField] private float _quillBobAmplitude = 6f;

    [Tooltip("Secondi per una oscillazione COMPLETA della piuma (un ciclo Rewind ne copre metà).")]
    [Min(0.02f)] [SerializeField] private float _quillBobPeriod = 1.6f;

    private VisualElement _root;
    private Label _label;
    private VisualElement _quill;

    private Tween _fadeTween;
    private Tween _bobTween;
    private IVisualElementScheduledItem _dots;
    private int _dotIndex;

    private void OnEnable()
    {
        VisualElement documentRoot = _document.rootVisualElement;
        _root = documentRoot.Q<VisualElement>("loading-root");
        _label = documentRoot.Q<Label>("loading-label");
        _quill = documentRoot.Q<VisualElement>("loading-quill");

        if (_root != null)
        {
            // Lo scheduler è legato al pannello, non alla visibilità dell'elemento: continuerebbe a
            // girare anche con l'overlay spento. Nasce quindi già in pausa e lo riaccende solo lo show.
            _dots = _root.schedule.Execute(AdvanceDots).Every(_dotIntervalMs);
            _dots.Pause();
        }

        // Parte nascosto: il primo frame della sessione non deve mostrare un overlay a schermo pieno
        // prima che qualcuno abbia chiesto una transizione.
        ApplyVisibility(false, instant: true);

        if (_visibilityChannel != null) _visibilityChannel.OnEventRaised += HandleVisibility;
    }

    private void OnDisable()
    {
        if (_visibilityChannel != null) _visibilityChannel.OnEventRaised -= HandleVisibility;

        _fadeTween.Stop();
        SetAnimationsRunning(false);
    }

    private void HandleVisibility(bool visible) => ApplyVisibility(visible, instant: false);

    private void ApplyVisibility(bool visible, bool instant)
    {
        if (_root == null) return;

        _fadeTween.Stop();
        SetAnimationsRunning(visible);

        if (instant)
        {
            _root.style.opacity = visible ? 1f : 0f;
            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            return;
        }

        if (visible)
        {
            // Il display va acceso PRIMA del tween: un elemento in DisplayStyle.None non viene
            // disegnato, quindi il fade-in non si vedrebbe affatto.
            _root.style.display = DisplayStyle.Flex;
            _fadeTween = Tween.Custom(_root, _root.style.opacity.value, 1f, _fadeDuration,
                static (el, v) => el.style.opacity = v, Ease.OutQuad);
            return;
        }

        _fadeTween = Tween.Custom(_root, _root.style.opacity.value, 0f, _fadeDuration,
            static (el, v) => el.style.opacity = v, Ease.InQuad)
            // Lo spegnimento del display va in coda al fade, altrimenti l'overlay resterebbe
            // trasparente ma cliccabile, rubando gli input alla scena appena caricata.
            .OnComplete(_root, static el => el.style.display = DisplayStyle.None);
    }

    /// <summary>
    /// Puntini e oscillazione vivono solo mentre l'overlay è a schermo: lasciarli girare a vuoto
    /// significherebbe far lavorare uno scheduler e un tween infinito per tutta la partita.
    /// </summary>
    private void SetAnimationsRunning(bool running)
    {
        _bobTween.Stop();

        if (!running)
        {
            _dots?.Pause();
            return;
        }

        // Si riparte sempre dalla parola nuda: riprendere da dove si era rimasti farebbe comparire
        // l'overlay con due puntini già scritti.
        _dotIndex = 0;
        if (_label != null) _label.text = DotStates[0];
        _dots?.Resume();

        if (_quill == null) return;

        // Stesso stampo del loop di idle dei pezzi del drago: il periodo configurato è
        // l'oscillazione completa, e un ciclo Rewind ne copre metà.
        // Tempo NON scalato: un overlay di caricamento non deve dipendere dal timeScale del gioco.
        _bobTween = Tween.Custom(_quill, -_quillBobAmplitude, _quillBobAmplitude,
            _quillBobPeriod * 0.5f,
            static (element, value) => element.style.translate = new Translate(0f, value),
            Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Rewind, useUnscaledTime: true);
    }

    private void AdvanceDots()
    {
        if (_label == null) return;

        _dotIndex = (_dotIndex + 1) % DotStates.Length;
        _label.text = DotStates[_dotIndex];
    }
}
