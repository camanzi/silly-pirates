using UnityEngine.UIElements;

public enum PassiveIndicatorSize
{
    Small,
    Large
}

public class PassiveIndicator : VisualElement
{
    public PassiveIndicator(PassiveAbilitySO passive, VisualTreeAsset template, PassiveHoverPopupController hoverPopup, PassiveIndicatorSize size = PassiveIndicatorSize.Small)
    {
        template.CloneTree(this);
        var icon = this.Q<VisualElement>("passive-icon");
        if (passive.Icon != null)
            icon.style.backgroundImage = new StyleBackground(passive.Icon);

        if (size == PassiveIndicatorSize.Large)
            icon.AddToClassList("passive-icon--large");

        RegisterCallback<PointerEnterEvent>(_ => hoverPopup?.Show(passive, this));
        RegisterCallback<PointerLeaveEvent>(_ => hoverPopup?.Hide());
    }
}
