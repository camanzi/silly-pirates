using UnityEngine;
using UnityEngine.UIElements;
using PrimeTween;
using System.Collections.Generic;

public class AwakeningCounterController : WorldSpaceContainer
{   
    [Header("Dependences")]
    [SerializeField] private InteractableGridElement _bindedMenuElement;

    private Label _currentCounterLabel;
    private Label _maxCounterLabel;
    private bool _isVisible = false;
    private Tween _visibilityTween;
    private bool _isRequested = false;
    private bool _isAllowedByState = true;

    protected override void OnEnable()
    {
        base.OnEnable();
        
        if (Container != null)
        {
            Container.style.opacity = 0f;
            Container.style.display = DisplayStyle.None;
        
            _currentCounterLabel = Container.Q<Label>("current-counter");
            _maxCounterLabel = Container.Q<Label>("max-counter");
        }
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();
    }

    public void SetStatePermission(bool isAllowed)
    {
        _isAllowedByState = isAllowed;
        ApplyVisibility();
    }

    public void ToggleRequested(bool show)
    {
        _isRequested = show;
        ApplyVisibility();
    }

    private void ApplyVisibility()
    {
        bool finalVisibility = _isRequested && _isAllowedByState;

        if (finalVisibility == _isVisible) return;
        
        _visibilityTween.Stop();
        _isVisible = finalVisibility;

        if (finalVisibility)
        {
            ShowCounters();
            _visibilityTween = Tween.Custom(0f, 1f, duration: .25f, ease: Ease.OutQuad, onValueChange: newVal => {
                Container.style.opacity = new StyleFloat(newVal);
            });
        }
        else
        {
            _visibilityTween = Tween.Custom(Container.style.opacity.value, 0f, duration: .25f, ease: Ease.OutQuad, onValueChange: newVal => {
                Container.style.opacity = new StyleFloat(newVal);
            }).OnComplete(() => {
                Container.style.display = DisplayStyle.None;
            });
        }
    }

    private void ShowCounters()
    {
        UpdateCounters();
        
        if (_bindedMenuElement is not IAwakable awakableElement)
            return;

        awakableElement.OnDataChanged -= UpdateCounters;
        awakableElement.OnDataChanged += UpdateCounters;
    }

    private void UpdateCounters()
    {
        if (_bindedMenuElement is not IAwakable awakableElement)
        {
            Container.style.display = DisplayStyle.None;
            return;
        }

        _maxCounterLabel.text = awakableElement.MaxAwakeningPoints.ToString();
        _currentCounterLabel.text = awakableElement.CurrentAwakeningPoints.ToString();
        Container.style.display = DisplayStyle.Flex;
    }
}
