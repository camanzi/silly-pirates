using System.Collections.Generic;
using System.Threading;
using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Animates the pieces of the main menu illustration (the "Moving Group" from the Figma mockup).
///
/// Two phases. The ENTRANCE assembles the drake from the bottom, one piece at a time, starting with
/// the ones closest to the horizontal centre and finishing with the ones on the edges, as if the
/// drawing were being put together on the spot. The IDLE that follows is an infinite, per-layer
/// staggered oscillation, which takes the "still image" feel off the screen without being
/// distracting.
///
/// The entrance ORDER is derived from the layers' centres at resolved layout, not from a hand-
/// written list: this is why <see cref="PlayIntroAsync"/> must be called after the first
/// GeometryChangedEvent, while <see cref="Bind"/> can (and must) be called earlier, to hide the
/// pieces in the very same frame the document appears.
///
/// Everything runs on UNSCALED time: a menu must not depend on the game's timeScale.
/// </summary>
public class MainMenuDrakeAnimator : MonoBehaviour
{
    /// <summary>
    /// Motion state of a layer. Needed because X and Y are separate tweens writing the same
    /// 'translate' property: without a point where they are summed, the last of the two would
    /// overwrite the other.
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
    /// Exposed because the controller uses the same parameters to bring the menu entries in right
    /// after the drake: a single reference asset instead of two fields to keep in sync by hand.
    /// </summary>
    public MainMenuMotionSO Motion => _motion;

    /// <summary>
    /// Collects the layers and immediately brings them to their starting state (invisible and
    /// shifted downward). Call from the controller's OnEnable: waiting for the first layout would
    /// mean the pieces are visible in the right place for a frame before disappearing.
    /// </summary>
    public void Bind(VisualElement stage)
    {
        _stage = stage;
        _layers.Clear();

        if (stage == null || _motion == null)
        {
            Debug.LogError(
                $"[{nameof(MainMenuDrakeAnimator)}] missing stage or {nameof(MainMenuMotionSO)}: " +
                "the illustration stays still.", this);
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
    /// Assembles the drake. Must be called at resolved layout: before that, every layer's rect is
    /// zero and sorting by distance from the centre would give a random-looking sequence.
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

        // The Sequence is also stopped by OnDisable, and in that case the await still returns:
        // without this check, the idle loops would start on a screen that has already been torn down.
        if (token.IsCancellationRequested || !isActiveAndEnabled) return;

        StartIdle();
    }

    /// <summary>Total duration of the entrance, used by the controller to queue the menu entries after it.</summary>
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
    /// Sorts by each piece's horizontal centre distance from the stage centre: whatever sits at the
    /// centre assembles first, whatever touches the vertical edges closes the sequence.
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

            // The configured period is the FULL oscillation; a Rewind cycle covers half of it.
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
