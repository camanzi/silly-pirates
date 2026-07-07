using UnityEngine.UIElements;

public class PassiveIndicator : VisualElement
{
    public PassiveIndicator(PassiveAbilitySO passive, VisualTreeAsset template, PassiveHoverPopupController hoverPopup)
    {
        template.CloneTree(this);
        var icon = this.Q<VisualElement>("passive-icon");
        if (passive.Icon != null)
            icon.style.backgroundImage = new StyleBackground(passive.Icon);

        RegisterCallback<PointerEnterEvent>(_ => hoverPopup?.Show(passive, this));
        RegisterCallback<PointerLeaveEvent>(_ => hoverPopup?.Hide());
    }
}
