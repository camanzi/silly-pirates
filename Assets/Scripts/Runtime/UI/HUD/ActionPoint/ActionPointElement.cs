using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class ActionPointElement : VisualElement
{
    private const string USS_CLASS_AP_AVAILABLE = "ap-element--available";
    private const string USS_CLASS_AP_CONSUMED = "ap-element--consumed";

    private Sprite _availableSprite;
    private Sprite _consumedSprite;
    private Tween _hoverGlowTween;
    public bool IsConsumed { get; private set; }

    public ActionPointElement() { }

    public void Setup(Sprite available, Sprite consumed)
    {
        _availableSprite = available;
        _consumedSprite = consumed;
        
        AddToClassList("ap-element");
        SetState(false);
    }

    public void SetState(bool isConsumed)
    {
        IsConsumed = isConsumed;
        style.backgroundImage = new StyleBackground(isConsumed ? _consumedSprite : _availableSprite);
    }

    public void StartHoverGlow()
    {
        _hoverGlowTween.Stop();
        style.unityBackgroundImageTintColor = new StyleColor(Color.white);
        _hoverGlowTween = Tween.Custom(this, Color.white, Color.red, duration: 1f,
            cycles: -1, cycleMode: CycleMode.Rewind,
            onValueChange: (target, val) => target.style.unityBackgroundImageTintColor = new StyleColor(val));
    }

    public void StopHoverGlow()
    {
        _hoverGlowTween.Stop();
        style.unityBackgroundImageTintColor = new StyleColor(Color.white);
    }
}
