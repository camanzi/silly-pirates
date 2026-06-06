using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

public class CountersController : WorldSpaceContainer
{
    [SerializeField] private InteractableGridElement _bindedMenuElement;

    private Label _currentLabel;
    private Label _maxLabel;
    private Label _divider;
    private Label _previewLabel;
    private IAwakable _awakable;
    private Tween _hoverPreviewTween;

    protected override void Awake()
    {
        base.Awake();
        _awakable = _bindedMenuElement as IAwakable;
        
        // Cache dei riferimenti
        _currentLabel = Container.Q<Label>("current-counter");
        _maxLabel = Container.Q<Label>("max-counter");
        _divider = Container.Q<Label>("divider");
        _previewLabel = Container.Q<Label>("awakening-preview");
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (_awakable != null)
        {
            Container.style.opacity = 0f;
            Container.style.display = DisplayStyle.None;

            _awakable.OnAwakeningCountersChanged += UpdateUI;
            _awakable.OnCooldownChanged += HandleCooldownTransition;
            _awakable.OnAwakeningHoverPreview += HandleHoverPreview;
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (_awakable != null)
        {
            _awakable.OnAwakeningCountersChanged -= UpdateUI;
            _awakable.OnCooldownChanged -= HandleCooldownTransition;
            _awakable.OnAwakeningHoverPreview -= HandleHoverPreview;
        }
    }

    protected override void ShowUI()
    {
        base.ShowUI();
        UpdateUI();
    }

    private void HandleCooldownTransition(int cooldown) => UpdateUI();

    protected override void RefreshUI() => UpdateUI();

    private void HandleHoverPreview(int amount)
    {
        _hoverPreviewTween.Stop();
        if (amount <= 0)
        {
            _previewLabel.style.opacity = new StyleFloat(0f);
            return;
        }
        _previewLabel.text = $"+{amount}";
        _hoverPreviewTween = Tween.Custom(this, 0.25f, 1f, duration: 0.35f,
            cycles: -1, cycleMode: CycleMode.Rewind,
            onValueChange: (self, val) => self._previewLabel.style.opacity = new StyleFloat(val));
    }

    private void UpdateUI()
    {
        if (_awakable == null) return;

        if (_awakable.Cooldown > 0)
        {
            _currentLabel.text = _awakable.Cooldown.ToString();
            _currentLabel.AddToClassList("cooldown");
            _divider.style.display = DisplayStyle.None;
            _maxLabel.style.display = DisplayStyle.None;
        }
        else
        {
            _currentLabel.text = _awakable.CurrentAwakeningPoints.ToString();
            _currentLabel.RemoveFromClassList("cooldown");
            _divider.style.display = DisplayStyle.Flex;
            _maxLabel.style.display = DisplayStyle.Flex;
            _maxLabel.text = _awakable.MaxAwakeningPoints.ToString();
        }

        Container.style.display = DisplayStyle.Flex;
    }
}
