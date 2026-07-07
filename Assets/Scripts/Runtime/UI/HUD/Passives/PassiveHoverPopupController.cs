using UnityEngine;
using UnityEngine.UIElements;

public class PassiveHoverPopupController : MonoBehaviour
{
    [SerializeField] private UIDocument _hudDocument;
    private VisualElement _root, _container, _iconElement;
    private Label _nameLabel, _descriptionLabel;

    private void Awake() => InitUI();

    private void InitUI()
    {
        if (_container != null) return;
        _root = _hudDocument.rootVisualElement;
        _container = _root.Q<VisualElement>("passive-popup-container");
        _iconElement = _root.Q<VisualElement>("passive-popup-icon");
        _nameLabel = _root.Q<Label>("passive-popup-name");
        _descriptionLabel = _root.Q<Label>("passive-popup-description");
    }

    public void Show(PassiveAbilitySO passive, VisualElement anchor)
    {
        InitUI();
        if (passive == null || anchor == null) return;
        if (passive.Icon != null) _iconElement.style.backgroundImage = new StyleBackground(passive.Icon);
        _nameLabel.text = passive.DisplayName;
        _descriptionLabel.text = passive.Description;
        _container.style.display = DisplayStyle.Flex;
        PositionNear(anchor);
    }

    public void Hide()
    {
        if (_container != null) _container.style.display = DisplayStyle.None;
    }

    private void PositionNear(VisualElement anchor)
    {
        Vector2 worldTop = anchor.LocalToWorld(new Vector2(anchor.layout.width / 2f, 0));
        Vector2 local = _root.WorldToLocal(worldTop);
        _container.style.left = local.x - (_container.resolvedStyle.width / 2f);
        _container.style.top = local.y - _container.resolvedStyle.height - 8f;
    }
}
