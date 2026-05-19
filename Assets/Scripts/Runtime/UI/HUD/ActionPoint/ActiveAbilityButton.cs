using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class ActiveAbilityButton : Button
{
    private VisualElement _iconElement;

    public ActiveAbilityButton()
    {
        AddToClassList("active-ability-button");

        _iconElement = new VisualElement();
        _iconElement.AddToClassList("active-ability-button__icon");
        Add(_iconElement);
    }

    public void SetAbilityIcon(Sprite icon)
    {
        _iconElement.style.backgroundImage = icon != null
            ? new StyleBackground(icon)
            : new StyleBackground();
    }
}
