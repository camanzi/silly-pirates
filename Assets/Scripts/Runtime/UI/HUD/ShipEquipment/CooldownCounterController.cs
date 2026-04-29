using UnityEngine;
using UnityEngine.UIElements;
using PrimeTween;

public class CooldownCounterController : WorldSpaceContainer
{   
    [Header("Dependences")]
    [SerializeField] private InteractableGridElement _bindedMenuElement;

    private Label _cooldownCounterLabel;

    protected override void OnEnable()
    {
        base.OnEnable();
        
        if (Container != null)
        {
            Container.style.opacity = 0f;
            Container.style.display = DisplayStyle.None;
        
            _cooldownCounterLabel = Container.Q<Label>("cooldown-counter");
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

        _cooldownCounterLabel.text = awakableElement.CurrentAwakeningPoints.ToString();
        Container.style.display = DisplayStyle.Flex;
    }
}
