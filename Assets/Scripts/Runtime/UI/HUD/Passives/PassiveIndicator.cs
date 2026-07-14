using UnityEngine.UIElements;

public enum PassiveIndicatorSize
{
    Small,
    Large
}

public class PassiveIndicator : VisualElement
{
    private VisualElement _stackBadge;
    private Label _stackBadgeLabel;

    public PassiveIndicator(PassiveAbilitySO passive, VisualTreeAsset template, PassiveHoverPopupController hoverPopup, PassiveIndicatorSize size = PassiveIndicatorSize.Small)
    {
        template.CloneTree(this);
        var icon = this.Q<VisualElement>("passive-icon");
        var iconImage = this.Q<VisualElement>("icon-image");
        if (passive.Icon != null)
            iconImage.style.backgroundImage = new StyleBackground(passive.Icon);

        _stackBadge = this.Q<VisualElement>("stack-badge");
        _stackBadgeLabel = this.Q<Label>("stack-badge-label");

        if (size == PassiveIndicatorSize.Large)
        {
            icon.AddToClassList("passive-icon--large");
            iconImage.AddToClassList("icon-image--large");
            _stackBadge.AddToClassList("stack-badge--large");
            _stackBadgeLabel.AddToClassList("stack-badge-label--large");
        }

        RegisterCallback<PointerEnterEvent>(_ => hoverPopup?.Show(passive, this));
        RegisterCallback<PointerLeaveEvent>(_ => hoverPopup?.Hide());

        if (passive is IStackCountProvider stackProvider)
            UpdateStackBadge(stackProvider.CurrentStacks, stackProvider.MaxStacks);
    }

    public void UpdateStackBadge(int current, int max, bool alwaysShow = false)
    {
        if (!alwaysShow && (max <= 1 || current < 2))
        {
            _stackBadge.style.display = DisplayStyle.None;
            return;
        }

        _stackBadgeLabel.text = current.ToString();
        _stackBadge.style.display = DisplayStyle.Flex;
    }
}
