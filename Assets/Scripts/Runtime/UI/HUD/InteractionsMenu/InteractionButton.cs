using UnityEngine;
using UnityEngine.UIElements;
using PrimeTween;

[UxmlElement]
public partial class InteractionButton : VisualElement
{
    private static readonly string USS_CLASS_NAME = "interaction-button";
    private static readonly string USS_ICON_NAME = "interaction-button__icon";

    private VisualElement _iconElement;
    private InteractionActionSO _data;
    private IInteractableElement _bindedElement;
    private ITurnAgent _interactingAgent;
    private Tween _hoverTween;

    public InteractionButton()
    {
        AddToClassList(USS_CLASS_NAME);
        _iconElement = new VisualElement();
        _iconElement.AddToClassList(USS_ICON_NAME);
        Add(_iconElement);
        
        style.scale = new StyleScale(Vector2.one);
        
        RegisterCallback<PointerEnterEvent>(OnPointerEnter);
        RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        RegisterCallback<PointerDownEvent>(OnPointerClick);
    }

    public void SetData(InteractionActionSO data, IInteractableElement bindedElement, ITurnAgent interactingAgent, bool isEnabled)
    {
        _data = data;
        _bindedElement = bindedElement;
        _interactingAgent = interactingAgent;
        _iconElement.style.backgroundImage = new StyleBackground(data.Icon);
        
        style.opacity = isEnabled ? 1.0f : 0.3f;
        SetEnabled(isEnabled);
        
        tooltip = data.ActionName;
    }

    private void OnPointerEnter(PointerEnterEvent evt)
    {
        _hoverTween.Stop();
        
        Tween.Custom(Vector3.one, new Vector3(1.15f, 1.15f), duration: 0.15f, ease: Ease.OutBack, onValueChange: newVal => {
            _iconElement.style.scale = new StyleScale(new Scale(new Vector3(newVal.x, newVal.y, 1f)));
        });
    }

    private void OnPointerLeave(PointerLeaveEvent evt)
    {
        _hoverTween.Stop();

        Tween.Custom(_iconElement.resolvedStyle.scale.value, Vector3.one, duration: 0.15f, ease: Ease.OutQuad, onValueChange: newVal => {
            _iconElement.style.scale = new StyleScale(new Scale(new Vector3(newVal.x, newVal.y, 1f)));
        });
    }

    private void OnPointerClick(PointerDownEvent evt)
    {
        Tween.Custom(_iconElement.resolvedStyle.scale.value, new Vector3(0.9f, 0.9f), duration: 0.05f, cycleMode: CycleMode.Rewind, cycles: 2, onValueChange: newVal => {
            _iconElement.style.scale = new StyleScale(new Scale(new Vector3(newVal.x, newVal.y, 1f)));
        });

        _data.ExecuteAction(_bindedElement, _interactingAgent);
    }
}