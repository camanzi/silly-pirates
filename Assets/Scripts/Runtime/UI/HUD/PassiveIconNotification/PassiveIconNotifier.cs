using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

public class PassiveIconNotifier : WorldSpaceContainer
{
    [Header("Passive Notification")]
    [SerializeField] private PassiveNotificationEventChannel _channel;
    [Tooltip("The owning character's transform: only events carrying this Source are shown.")]
    [SerializeField] private Transform _owner;

    [Header("Animation")]
    [SerializeField] private float _duration = 1f;
    [SerializeField] private float _floatDistance = 20f;
    [SerializeField] private float _gainRotation = 15f;
    [SerializeField] private float _lossRotation = -15f;
    [SerializeField] private float _shrunkScale = 0.85f;

    private VisualElement _iconImage;
    private readonly Queue<PassiveNotificationEvent> _pending = new();
    private bool _isPlaying;
    private Sequence _sequence;

    protected override void Awake()
    {
        base.Awake();
        // No CloneTree() per event: the icon-image element is unique and is reused for every
        // notification, which brings the allocations per passive gain/loss down to zero.
        _iconImage = _uiDocument.rootVisualElement.Q<VisualElement>("icon-image");

        if (_owner == null)
        {
            _owner = transform.parent;
            Debug.LogError($"{GetType().Name}: no owner assigned, falling back to transform.parent.", this);
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (_channel != null) _channel.OnEventRaised += HandleNotification;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (_channel != null) _channel.OnEventRaised -= HandleNotification;

        // Stops the running animation and clears the queue: when the component re-enables there must be
        // no stale "playing" state left behind.
        _sequence.Stop();
        _pending.Clear();
        _isPlaying = false;
    }

    private void HandleNotification(PassiveNotificationEvent evt)
    {
        if (evt.Source != _owner || evt.Icon == null) return;

        if (_isPlaying)
            _pending.Enqueue(evt);
        else
            Play(evt);
    }

    private void Play(PassiveNotificationEvent evt)
    {
        _isPlaying = true;
        _iconImage.style.backgroundImage = new StyleBackground(evt.Icon);

        float fromScale = evt.WasAdded ? _shrunkScale : 1f;
        float toScale = evt.WasAdded ? 1f : _shrunkScale;
        float fromRotation = evt.WasAdded ? _gainRotation : 0f;
        float toRotation = evt.WasAdded ? 0f : _lossRotation;
        // In UI Toolkit +Y points down: a gain rises (negative delta), a loss falls (positive delta).
        // The same convention as PassiveNotificationManager.cs.
        float yDelta = evt.WasAdded ? -_floatDistance : _floatDistance;

        // Initial state consistent with the chosen direction, applied before starting the tween.
        _iconImage.style.scale = new StyleScale(new Scale(new Vector3(fromScale, fromScale, 1f)));
        _iconImage.style.rotate = new StyleRotate(new Rotate(new Angle(fromRotation, AngleUnit.Degree)));
        _iconImage.style.translate = new StyleTranslate(new Translate(0f, 0f));

        ToggleRequested(true);
        // ToggleRequested puts the container back to display:Flex, but left/top are still the ones from
        // before it was hidden: UpdateUIPosition() bails out early while the display is None
        // (WorldSpaceContainer.cs), so without this call the first frame would draw the icon in the corner
        // of the panel, before LateUpdate brings it back onto the character.
        UpdateUIPosition();

        _sequence = Sequence.Create(
                Tween.Custom(_iconImage, fromScale, toScale, _duration,
                    static (el, v) => el.style.scale = new StyleScale(new Scale(new Vector3(v, v, 1f))), Ease.OutBack))
            .Group(Tween.Custom(_iconImage, fromRotation, toRotation, _duration,
                    static (el, v) => el.style.rotate = new StyleRotate(new Rotate(new Angle(v, AngleUnit.Degree))), Ease.OutQuad))
            .Group(Tween.Custom(_iconImage, 0f, yDelta, _duration,
                    static (el, v) => el.style.translate = new StyleTranslate(new Translate(0f, v)), Ease.OutQuad))
            .OnComplete(this, static self => self.OnAnimationComplete());
        // NEVER tween the opacity here: it is ApplyVisibility()'s gate in the base class, and the two
        // tweens would fight over the same property.
    }

    private void OnAnimationComplete()
    {
        if (_pending.Count > 0)
        {
            Play(_pending.Dequeue());
            return;
        }

        _isPlaying = false;
        ToggleRequested(false);
    }
}
