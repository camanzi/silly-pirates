using UnityEngine;
using UnityEngine.UIElements;
using PrimeTween;
using System;

[UxmlElement]
public partial class EquipmentStatusElement : VisualElement
{
    private static readonly Color WhiteFull = new Color(1f, 1f, 1f, 1f);
    private static readonly Color WhiteEmpty = new Color(1f, 1f, 1f, 0.2f);
    private static readonly Color GoldFull = new Color(1f, 0.784f, 0.196f, 1f);
    private static readonly Color GoldEmpty = new Color(1f, 0.784f, 0.196f, 0.2f);
    private static readonly Color PreviewColor = new Color(0.267f, 1f, 0f, 0.7f);
    private static readonly Color RedFull  = new Color(1f, 0.2f, 0.2f, 1f);
    private static readonly Color RedEmpty = new Color(1f, 0.2f, 0.2f, 0.2f);

    private const float OuterRingRadius = 55f;
    private const float InnerRingRadius = 40f;
    private const float OuterRingWidth = 8f;
    private const float InnerRingWidth = 8f;
    private const float SegmentGapDeg = 6f;

    private VisualElement _iconElement;
    private VisualElement _radialMaskElement;
    private InteractionActionSO _data;
    private Sprite _cooldownIcon;
    private IInteractableElement _bindedElement;
    private ITurnAgent _interactingAgent;
    private IAwakable _awakable;
    private Action _onClickAction;
    private IntEventChannel _hoverChannel;
    private Tween _hoverTween;

    private int _hoverPreviewSegments;
    private int _prevCooldown;
    private int _initialCooldown;
    private bool _canExecute;

    public EquipmentStatusElement()
    {
        AddToClassList("equipment-status");
        pickingMode = PickingMode.Position;

        _iconElement = new VisualElement();
        _iconElement.AddToClassList("equipment-status__icon");
        _iconElement.pickingMode = PickingMode.Ignore;
        Add(_iconElement);

        _radialMaskElement = new VisualElement();
        _radialMaskElement.AddToClassList("equipment-status__mask");
        _radialMaskElement.pickingMode = PickingMode.Ignore;
        _radialMaskElement.generateVisualContent += OnGenerateRadialMask;
        Add(_radialMaskElement);

        generateVisualContent += OnGenerateVisualContent;

        RegisterCallback<PointerEnterEvent>(OnPointerEnter);
        RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        RegisterCallback<PointerDownEvent>(OnPointerClick);
    }

    public void SetData(InteractionActionSO mainAction, Sprite cooldownIcon,
                        IInteractableElement bindedElement, ITurnAgent agent,
                        IAwakable awakable, Action onClickAction,
                        IntEventChannel hoverChannel = null)
    {
        _data = mainAction;
        _cooldownIcon = cooldownIcon;
        _bindedElement = bindedElement;
        _interactingAgent = agent;
        _awakable = awakable;
        _onClickAction = onClickAction;
        _hoverChannel = hoverChannel;
        _prevCooldown = _awakable?.Cooldown ?? 0;
        _initialCooldown = _prevCooldown;

        tooltip = mainAction?.ActionName ?? string.Empty;
        RefreshAwakening();
    }

    public void RefreshCooldown(int cooldown)
    {
        if (_awakable == null) { MarkDirtyRepaint(); _radialMaskElement?.MarkDirtyRepaint(); return; }

        if (_prevCooldown == 0 && cooldown > 0)
            _initialCooldown = cooldown;
        _prevCooldown = cooldown;

        if (_cooldownIcon != null)
            _iconElement.style.backgroundImage = new StyleBackground(_cooldownIcon);

        _canExecute = false;
        _iconElement.style.opacity = 0.4f;
        _radialMaskElement.MarkDirtyRepaint();
        MarkDirtyRepaint();
    }

    public void RefreshAwakening()
    {
        if (_awakable == null) { MarkDirtyRepaint(); _radialMaskElement?.MarkDirtyRepaint(); return; }

        _prevCooldown = 0;

        Sprite icon = _data?.Icon;
        if (icon != null)
            _iconElement.style.backgroundImage = new StyleBackground(icon);

        _canExecute = _data.CanExecute(_bindedElement, _interactingAgent);
        _iconElement.style.opacity = _canExecute ? 1f : 0.7f;
        _radialMaskElement.MarkDirtyRepaint();
        MarkDirtyRepaint();
    }

    public void UpdateAgent(ITurnAgent agent) => _interactingAgent = agent;

    public void SetHoverPreview(int segments)
    {
        _hoverPreviewSegments = segments;
        MarkDirtyRepaint();
        _radialMaskElement.MarkDirtyRepaint();
    }

    private void GetPrimaryRingState(out int total, out int filled)
    {
        if (_awakable.IsOnCooldown)
        {
            int ic = _initialCooldown > 0 ? _initialCooldown : 1;
            total = ic;
            filled = ic - _awakable.Cooldown;
        }
        else
        {
            total = Mathf.Max(1, _awakable.MaxAwakeningPoints);
            filled = _awakable.CurrentAwakeningPoints;
        }
    }

    private void OnGenerateVisualContent(MeshGenerationContext ctx)
    {
        if (_awakable == null) return;

        var painter = ctx.painter2D;
        float cx = contentRect.width / 2f;
        float cy = contentRect.height / 2f;

        GetPrimaryRingState(out int primarySegments, out int primaryFilled);

        bool isOnCooldown = _awakable.IsOnCooldown;
        int ringFilled   = isOnCooldown ? _awakable.Cooldown : primaryFilled;
        Color fullColor  = isOnCooldown ? RedFull  : WhiteFull;
        Color emptyColor = isOnCooldown ? RedEmpty : WhiteEmpty;

        DrawRing(painter, cx, cy, OuterRingRadius, OuterRingWidth,
                 primarySegments, ringFilled, _hoverPreviewSegments,
                 fullColor, emptyColor, PreviewColor);

        if (_awakable.IsAwake && !_awakable.IsOnCooldown)
        {
            int overcapTotal = _awakable.OvercapLimit - _awakable.MaxAwakeningPoints;
            int overcapFilled = Mathf.Max(0, _awakable.CurrentAwakeningPoints - _awakable.MaxAwakeningPoints);
            if (overcapTotal > 0)
                DrawRing(painter, cx, cy, InnerRingRadius, InnerRingWidth,
                         overcapTotal, overcapFilled, _hoverPreviewSegments,
                         GoldFull, GoldEmpty, GoldFull);
        }
    }

    private void OnGenerateRadialMask(MeshGenerationContext ctx)
    {
        if (_awakable == null) return;

        GetPrimaryRingState(out int total, out int filled);
        int previewFilled = Mathf.Min(filled + _hoverPreviewSegments, total);
        float revealedDeg = total > 0 ? (float)previewFilled / total * 360f : 0f;
        if (revealedDeg >= 360f) return;

        var painter = ctx.painter2D;
        float cx = ctx.visualElement.contentRect.width / 2f;
        float cy = ctx.visualElement.contentRect.height / 2f;
        float radius = Mathf.Min(cx, cy);
        var center = new Vector2(cx, cy);
        painter.fillColor = new Color(0f, 0f, 0f, 0.75f);

        if (_awakable.IsOnCooldown)
        {
            // Mask covers the remaining cooldown arc (top, clockwise) so it clears
            // counter-clockwise, matching how red segments empty in OnGenerateVisualContent.
            float remainingDeg = 360f - revealedDeg;
            if (remainingDeg >= 360f)
            {
                painter.BeginPath();
                painter.Arc(center, radius, 0f, 360f);
                painter.Fill();
                return;
            }
            float unrevealedEnd = -90f + remainingDeg;
            painter.BeginPath();
            painter.MoveTo(center);
            painter.LineTo(new Vector2(cx, cy - radius));
            painter.Arc(center, radius, -90f, unrevealedEnd, ArcDirection.Clockwise);
            painter.ClosePath();
            painter.Fill();
        }
        else
        {
            if (revealedDeg <= 0f)
            {
                painter.BeginPath();
                painter.Arc(center, radius, 0f, 360f);
                painter.Fill();
                return;
            }

            float unrevealedStart = -90f + revealedDeg;
            float arcStartX = cx + radius * Mathf.Cos(unrevealedStart * Mathf.Deg2Rad);
            float arcStartY = cy + radius * Mathf.Sin(unrevealedStart * Mathf.Deg2Rad);

            painter.BeginPath();
            painter.MoveTo(center);
            painter.LineTo(new Vector2(arcStartX, arcStartY));
            painter.Arc(center, radius, unrevealedStart, 270f, ArcDirection.Clockwise);
            painter.ClosePath();
            painter.Fill();
        }
    }

    private void DrawRing(Painter2D painter, float cx, float cy, float radius, float width,
                          int totalSegments, int filled, int preview,
                          Color fullColor, Color emptyColor, Color previewColor)
    {
        if (totalSegments <= 0) return;

        float gapDeg = totalSegments > 1 ? SegmentGapDeg : 0f;
        float segmentDeg = (360f - gapDeg * totalSegments) / totalSegments;
        if (segmentDeg <= 0f) return;

        painter.lineWidth = width;
        var center = new Vector2(cx, cy);

        for (int i = 0; i < totalSegments; i++)
        {
            float segStart = -90f + i * (segmentDeg + gapDeg);
            float segEnd = segStart + segmentDeg;

            if (i < filled)
                painter.strokeColor = fullColor;
            else if (i < filled + preview)
                painter.strokeColor = previewColor;
            else
                painter.strokeColor = emptyColor;

            painter.BeginPath();
            painter.Arc(center, radius, segStart, segEnd);
            painter.Stroke();
        }
    }

    private void OnPointerEnter(PointerEnterEvent evt)
    {
        if (!_canExecute || _data == null) return;
        _hoverTween.Stop();
        Tween.Custom(Vector3.one, new Vector3(1.15f, 1.15f, 1f), duration: 0.15f, ease: Ease.OutBack,
            onValueChange: v => _iconElement.style.scale = new StyleScale(new Scale(v)));

        _hoverChannel?.RaiseEvent(_data.GetHoverApCost(_bindedElement, _interactingAgent));
        int awakePreview = _data.GetHoverAwakeningPreview(_bindedElement, _interactingAgent);
        if (awakePreview > 0)
            (_bindedElement as IAwakable)?.OnAwakeningHoverPreview?.Invoke(awakePreview);
    }

    private void OnPointerLeave(PointerLeaveEvent evt)
    {
        _hoverTween.Stop();
        Tween.Custom(_iconElement.resolvedStyle.scale.value, Vector3.one, duration: 0.15f, ease: Ease.OutQuad,
            onValueChange: v => _iconElement.style.scale = new StyleScale(new Scale(v)));

        _hoverChannel?.RaiseEvent(0);
        (_bindedElement as IAwakable)?.OnAwakeningHoverPreview?.Invoke(0);
    }

    private void OnPointerClick(PointerDownEvent evt)
    {
        if (!_canExecute || _data == null) return;
        Tween.Custom(_iconElement.resolvedStyle.scale.value, new Vector3(0.9f, 0.9f, 1f), duration: 0.05f,
            cycleMode: CycleMode.Rewind, cycles: 2,
            onValueChange: v => _iconElement.style.scale = new StyleScale(new Scale(v)));

        bool result = _data.ExecuteAction(_bindedElement, _interactingAgent);
        if (result) _onClickAction?.Invoke();
    }
}
