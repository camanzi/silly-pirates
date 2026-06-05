using System.Collections.Generic;
using System.Threading;
using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

public class FlavorTextBannerController : MonoBehaviour
{
    [SerializeField] private UIDocument _uiDocument;

    [Header("Animations Settings")]
    [SerializeField] private float _animationDuration = 0.5f;

    private VisualElement _root;
    private Label _label;

    private readonly Queue<string> _queue = new();
    private bool _isProcessing;
    private CancellationTokenSource _cts;
    private Awaitable _processingLoop;

    private void Awake()
    {
        var rootVe = _uiDocument.rootVisualElement;
        _root = rootVe.Q<VisualElement>("flavor-root");
        _label = rootVe.Q<Label>("flavor-label");
    }

    private void OnEnable() => _cts = new CancellationTokenSource();

    private void OnDisable()
    {
        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
        _queue.Clear();
        _root?.RemoveFromClassList("visible");
    }

    public void Enqueue(string text)
    {
        _queue.Enqueue(text);
        if (!_isProcessing)
            _processingLoop = ProcessQueueAsync(_cts.Token);
    }

    private async Awaitable ProcessQueueAsync(CancellationToken ct)
    {
        _isProcessing = true;
        try
        {
            while (_queue.TryDequeue(out string text) && !ct.IsCancellationRequested)
            {
                _label.text = text;
                _root.AddToClassList("visible");
                await Tween.Custom(_root, 0f, 1f, _animationDuration, static (el, v) => el.style.opacity = v, Ease.OutBack);
                await Awaitable.WaitForSecondsAsync(2f, ct);
                await Tween.Custom(_root, _root.resolvedStyle.opacity, 0f, _animationDuration, static (el, v) => el.style.opacity = v, Ease.OutBack);
                _root.RemoveFromClassList("visible");
            }
        }
        catch (System.OperationCanceledException) { }
        finally
        {
            _isProcessing = false;
            _root?.RemoveFromClassList("visible");
        }
    }
}
