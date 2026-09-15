using System.Collections.Generic;
using System.Threading;
using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Anima i pezzi dell'illustrazione del main menu (il gruppo "Moving Group" del mockup Figma).
///
/// Due fasi. L'ENTRATA compone il drago dal basso un pezzo alla volta, partendo da quelli piu' vicini
/// al centro orizzontale e chiudendo con quelli sui bordi, come se il disegno venisse assemblato sul
/// momento. L'IDLE che segue e' un'oscillazione infinita e sfasata per layer, che toglie alla
/// schermata l'aria di immagine ferma senza distrarre.
///
/// L'ordine di entrata si ricava dai centri dei layer a layout risolto, non da una lista scritta a
/// mano: e' per questo che <see cref="PlayIntroAsync"/> va chiamata dopo il primo GeometryChangedEvent,
/// mentre <see cref="Bind"/> puo' (e deve) essere chiamata prima, per nascondere i pezzi nel frame
/// stesso in cui il documento compare.
///
/// Tutto gira a tempo NON scalato: un menu non deve dipendere dal timeScale del gioco.
/// </summary>
public class MainMenuDrakeAnimator : MonoBehaviour
{
    /// <summary>
    /// Stato di movimento di un layer. Serve perche' X e Y sono tween separati che scrivono la stessa
    /// proprieta' 'translate': senza un punto in cui sommarli, l'ultimo dei due cancellerebbe l'altro.
    /// </summary>
    private sealed class LayerState
    {
        public VisualElement Element;
        private float _x;
        private float _y;

        public void SetX(float value) { _x = value; Apply(); }
        public void SetY(float value) { _y = value; Apply(); }
        public void Reset() { _x = 0f; _y = 0f; Apply(); }

        private void Apply() => Element.style.translate = new Translate(_x, _y);
    }

    [SerializeField] private MainMenuMotionSO _motion;

    private readonly List<LayerState> _layers = new();
    private readonly List<Tween> _idleTweens = new();
    private Sequence _intro;
    private VisualElement _stage;

    /// <summary>
    /// Esposto perche' il controller usa gli stessi parametri per far entrare le voci di menu subito
    /// dopo il drago: un solo asset di riferimento invece di due campi da tenere allineati a mano.
    /// </summary>
    public MainMenuMotionSO Motion => _motion;

    /// <summary>
    /// Raccoglie i layer e li porta subito allo stato di partenza (invisibili e spostati in basso).
    /// Da chiamare nell'OnEnable del controller: se si aspettasse il primo layout, i pezzi
    /// sarebbero visibili al posto giusto per un frame prima di sparire.
    /// </summary>
    public void Bind(VisualElement stage)
    {
        _stage = stage;
        _layers.Clear();

        if (stage == null || _motion == null)
        {
            Debug.LogError(
                $"[{nameof(MainMenuDrakeAnimator)}] stage o {nameof(MainMenuMotionSO)} mancanti: " +
                "l'illustrazione resta ferma.", this);
            return;
        }

        stage.Query<VisualElement>(className: "drake-layer").ForEach(element =>
        {
            LayerState state = new() { Element = element };
            _layers.Add(state);

            element.style.opacity = 0f;
            state.SetY(_motion.RiseDistance);
        });
    }

    /// <summary>
    /// Compone il drago. Va chiamata a layout risolto: prima, i rettangoli dei layer sono tutti a zero
    /// e l'ordinamento per distanza dal centro darebbe una sequenza casuale.
    /// </summary>
    public async Awaitable PlayIntroAsync(CancellationToken token)
    {
        if (_layers.Count == 0 || _motion == null) return;

        SortByDistanceFromCentre();

        _intro.Stop();
        _intro = Sequence.Create(useUnscaledTime: true);

        for (int i = 0; i < _layers.Count; i++)
        {
            LayerState layer = _layers[i];
            float at = i * _motion.Stagger;

            _intro = _intro.Insert(at, Tween.Custom(layer, _motion.RiseDistance, 0f, _motion.PieceDuration,
                static (state, value) => state.SetY(value), _motion.PieceEase, useUnscaledTime: true));

            _intro = _intro.Insert(at, Tween.Custom(layer.Element, 0f, 1f, _motion.PieceDuration,
                static (element, value) => element.style.opacity = value, _motion.PieceEase,
                useUnscaledTime: true));
        }

        await _intro;

        // La Sequence viene fermata anche da OnDisable, e in quel caso l'await ritorna comunque:
        // senza questo controllo si avvierebbero i loop di idle su una schermata gia' smontata.
        if (token.IsCancellationRequested || !isActiveAndEnabled) return;

        StartIdle();
    }

    /// <summary>Durata complessiva dell'entrata, usata dal controller per accodare le voci di menu.</summary>
    public float IntroDuration =>
        _motion == null || _layers.Count == 0
            ? 0f
            : (_layers.Count - 1) * _motion.Stagger + _motion.PieceDuration;

    private void OnDisable()
    {
        _intro.Stop();

        for (int i = 0; i < _idleTweens.Count; i++)
            _idleTweens[i].Stop();

        _idleTweens.Clear();
    }

    /// <summary>
    /// Ordina per distanza orizzontale del centro del pezzo dal centro dello stage: chi sta al centro
    /// si compone per primo, chi tocca i bordi verticali chiude la sequenza.
    /// </summary>
    private void SortByDistanceFromCentre()
    {
        float centre = _stage.layout.width * 0.5f;

        _layers.Sort((a, b) =>
            Mathf.Abs(a.Element.layout.center.x - centre)
                .CompareTo(Mathf.Abs(b.Element.layout.center.x - centre)));
    }

    private void StartIdle()
    {
        for (int i = 0; i < _layers.Count; i++)
        {
            LayerState layer = _layers[i];
            layer.Reset();

            if (!_motion.TryGetIdle(layer.Element.name, out MainMenuMotionSO.LayerIdle idle)) continue;

            // Il periodo configurato e' l'oscillazione COMPLETA; un ciclo Rewind ne copre meta'.
            if (idle.amplitude.x != 0f && idle.period.x > 0f)
                _idleTweens.Add(Tween.Custom(layer, -idle.amplitude.x, idle.amplitude.x,
                    idle.period.x * 0.5f, static (state, value) => state.SetX(value), idle.ease,
                    cycles: -1, cycleMode: CycleMode.Rewind, startDelay: idle.startDelay,
                    useUnscaledTime: true));

            if (idle.amplitude.y != 0f && idle.period.y > 0f)
                _idleTweens.Add(Tween.Custom(layer, -idle.amplitude.y, idle.amplitude.y,
                    idle.period.y * 0.5f, static (state, value) => state.SetY(value), idle.ease,
                    cycles: -1, cycleMode: CycleMode.Rewind, startDelay: idle.startDelay,
                    useUnscaledTime: true));

            if (idle.rotationAmplitude != 0f && idle.rotationPeriod > 0f)
                _idleTweens.Add(Tween.Custom(layer.Element, -idle.rotationAmplitude, idle.rotationAmplitude,
                    idle.rotationPeriod * 0.5f,
                    static (element, value) => element.style.rotate = new Rotate(value), idle.ease,
                    cycles: -1, cycleMode: CycleMode.Rewind, startDelay: idle.startDelay,
                    useUnscaledTime: true));
        }
    }
}
