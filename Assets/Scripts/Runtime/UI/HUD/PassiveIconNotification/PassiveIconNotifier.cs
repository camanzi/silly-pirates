using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

public class PassiveIconNotifier : WorldSpaceContainer
{
    [Header("Passive Notification")]
    [SerializeField] private PassiveNotificationEventChannel _channel;
    [Tooltip("Transform del personaggio proprietario: solo gli eventi con questo Source vengono mostrati.")]
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
        // Nessun CloneTree() per evento: l'elemento icon-image è unico e viene riusato per ogni
        // notifica, così da azzerare le allocazioni per ogni guadagno/perdita di passiva.
        _iconImage = _uiDocument.rootVisualElement.Q<VisualElement>("icon-image");

        if (_owner == null)
        {
            _owner = transform.parent;
            Debug.LogError($"{GetType().Name}: nessun owner assegnato, uso transform.parent come fallback.", this);
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

        // Ferma l'animazione in corso e svuota la coda: al riabilitarsi del componente
        // non deve restare uno stato "in riproduzione" stantio.
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
        // In UI Toolkit +Y punta verso il basso: guadagno sale (delta negativo),
        // perdita scende (delta positivo). Stessa convenzione di PassiveNotificationManager.cs.
        float yDelta = evt.WasAdded ? -_floatDistance : _floatDistance;

        // Stato iniziale coerente con la direzione scelta, applicato prima di avviare il tween.
        _iconImage.style.scale = new StyleScale(new Scale(new Vector3(fromScale, fromScale, 1f)));
        _iconImage.style.rotate = new StyleRotate(new Rotate(new Angle(fromRotation, AngleUnit.Degree)));
        _iconImage.style.translate = new StyleTranslate(new Translate(0f, 0f));

        ToggleRequested(true);
        // ToggleRequested rimette il container a display:Flex, ma left/top sono ancora quelli di
        // prima che venisse nascosto: UpdateUIPosition() esce in anticipo finché il display è None
        // (WorldSpaceContainer.cs), quindi senza questa chiamata il primo frame disegnerebbe l'icona
        // nell'angolo del pannello, prima che il LateUpdate la riporti sul personaggio.
        UpdateUIPosition();

        _sequence = Sequence.Create(
                Tween.Custom(_iconImage, fromScale, toScale, _duration,
                    static (el, v) => el.style.scale = new StyleScale(new Scale(new Vector3(v, v, 1f))), Ease.OutBack))
            .Group(Tween.Custom(_iconImage, fromRotation, toRotation, _duration,
                    static (el, v) => el.style.rotate = new StyleRotate(new Rotate(new Angle(v, AngleUnit.Degree))), Ease.OutQuad))
            .Group(Tween.Custom(_iconImage, 0f, yDelta, _duration,
                    static (el, v) => el.style.translate = new StyleTranslate(new Translate(0f, v)), Ease.OutQuad))
            .OnComplete(this, static self => self.OnAnimationComplete());
        // NON tweenare mai l'opacità qui: è il gate di ApplyVisibility() nella base class,
        // altrimenti i due tween si contenderebbero la stessa proprietà.
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
