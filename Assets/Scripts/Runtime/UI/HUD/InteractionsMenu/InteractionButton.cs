using UnityEngine;
using UnityEngine.UIElements;
using PrimeTween;
using System;

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
    private Action _onClickAction;

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

    public void SetData(InteractionActionSO data, IInteractableElement bindedElement, ITurnAgent interactingAgent, Action onClickAction)
    {
        _data = data;
        _bindedElement = bindedElement;
        _interactingAgent = interactingAgent;
        _iconElement.style.backgroundImage = new StyleBackground(data.Icon);
        _onClickAction = onClickAction;

        tooltip = data.ActionName;
    }

    public void UpdateData(InteractionActionSO data)
    {
        _data = data;
        tooltip = data.ActionName;

        Sequence.Create()
            .Chain(Tween.Custom(this, 1f, 0.7f, duration: 0.1f,
                onValueChange: (self, v) => self.style.scale = new StyleScale(new Scale(new Vector3(v, v, 1f)))))
            .ChainCallback(() => _iconElement.style.backgroundImage = new StyleBackground(data.Icon))
            .Chain(Tween.Custom(this, 0.7f, 1f, duration: 0.15f, ease: Ease.OutBack,
                onValueChange: (self, v) => self.style.scale = new StyleScale(new Scale(new Vector3(v, v, 1f)))));
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

        bool result = _data.ExecuteAction(_bindedElement, _interactingAgent);
        
        if (result) _onClickAction?.Invoke();
    }
}