using System.Collections.Generic;
using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

public class PassiveNotificationManager : MonoBehaviour
{
    [SerializeField] private PassiveNotificationEventChannel _channel;
    [SerializeField] private VisualTreeAsset _notificationTemplate;

    [Header("Animation Settings")]
    [SerializeField] private float _appearDuration = 0.2f;
    [SerializeField] private float _moveFadeDuration = 0.5f;
    [SerializeField] private float _moveDistance = 10f;

    private const float MinGapSeconds = 0.5f;

    private class PerCharacterState
    {
        public readonly Queue<PassiveNotificationEvent> Pending = new();
        public float LastStartTime = float.NegativeInfinity;
    }

    private class ActivePopup
    {
        public VisualElement Element;
        public Transform Source;
        public float YOffset;
    }

    private readonly Dictionary<Transform, PerCharacterState> _states = new();
    private readonly List<ActivePopup> _activePopups = new();
    private readonly List<Transform> _deadSources = new();
    private VisualElement _root;
    private Camera _camera;

    private void Awake()
    {
        _camera = Camera.main;
    }

    private void Start()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        _root.pickingMode = PickingMode.Ignore;
    }

    private void OnEnable()
    {
        if (_channel != null) _channel.OnEventRaised += HandleNotification;
    }

    private void OnDisable()
    {
        if (_channel != null) _channel.OnEventRaised -= HandleNotification;
    }

    private void LateUpdate()
    {
        if (_root == null || _camera == null) return;

        _deadSources.Clear();
        foreach (var kvp in _states)
        {
            if (kvp.Key == null)
            {
                _deadSources.Add(kvp.Key);
                continue;
            }
            var state = kvp.Value;
            if (state.Pending.Count > 0 && Time.time - state.LastStartTime >= MinGapSeconds)
                SpawnNotification(state.Pending.Dequeue(), state);
        }
        foreach (var key in _deadSources) _states.Remove(key);

        for (int i = _activePopups.Count - 1; i >= 0; i--)
        {
            var active = _activePopups[i];
            if (active.Element.parent == null)
            {
                _activePopups.RemoveAt(i);
                continue;
            }
            if (active.Source == null) continue;
            Vector2 panelPos = RuntimePanelUtils.CameraTransformWorldToPanel(
                _root.panel, active.Source.position, _camera);
            active.Element.style.left = panelPos.x;
            active.Element.style.top = panelPos.y + active.YOffset;
        }
    }

    public void HandleNotification(PassiveNotificationEvent evt)
    {
        if (_root == null || evt.Source == null) return;

        if (!_states.TryGetValue(evt.Source, out var state))
        {
            state = new PerCharacterState();
            _states[evt.Source] = state;
        }

        if (state.Pending.Count == 0 && Time.time - state.LastStartTime >= MinGapSeconds)
            SpawnNotification(evt, state);
        else
            state.Pending.Enqueue(evt);
    }

    private void SpawnNotification(PassiveNotificationEvent evt, PerCharacterState state)
    {
        state.LastStartTime = Time.time;

        var popup = _notificationTemplate.CloneTree();
        popup.pickingMode = PickingMode.Ignore;

        var label = popup.Q<Label>("passive-label");
        // Stessa soglia del badge in PassiveIndicator, così i due elementi concordano.
        label.text = evt.StackCount >= 2 ? $"{evt.DisplayName} x{evt.StackCount}" : evt.DisplayName;
        label.style.color = evt.WasAdded ? Color.white : Color.red;

        popup.style.opacity = 0;
        _root.Add(popup);

        float yDelta = evt.WasAdded ? -_moveDistance : _moveDistance;
        var active = new ActivePopup { Element = popup, Source = evt.Source, YOffset = 0f };
        _activePopups.Add(active);

        Sequence.Create()
            .Chain(Tween.Custom(popup, 0f, 1f, _appearDuration,
                static (el, v) => el.style.opacity = v, Ease.Linear))
            .Chain(Tween.Custom(active, 0f, yDelta, _moveFadeDuration,
                static (ap, v) => ap.YOffset = v, Ease.Linear))
            .Group(Tween.Custom(popup, 1f, 0f, _moveFadeDuration,
                static (el, v) => el.style.opacity = v, Ease.Linear))
            .OnComplete(popup, static el => el.RemoveFromHierarchy());
    }
}
