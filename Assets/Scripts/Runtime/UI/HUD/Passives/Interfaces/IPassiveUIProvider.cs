using UnityEngine.UIElements;

public interface IPassiveUIProvider {
    VisualElement CreateUIElement(VisualTreeAsset template);
    VisualTreeAsset GetTemplate();
}