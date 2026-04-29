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
    
    protected override void ShowUI()
    {
        base.ShowUI();
        ShowCounters();
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
