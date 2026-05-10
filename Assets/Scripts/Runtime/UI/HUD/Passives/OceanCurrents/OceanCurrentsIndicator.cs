using UnityEngine.UIElements;

public class OceanCurrentsIndicator : VisualElement, IDisposableUI
{
    private VisualElement _waveIcon;
    private IOceanCurrentsUIReporter _reporter;

    public OceanCurrentsIndicator(IOceanCurrentsUIReporter reporter, VisualTreeAsset uxml)
    {
        _reporter = reporter;
        uxml.CloneTree(this);

        style.height = Length.Percent(100);
        style.aspectRatio = 1;
        style.marginRight = 5;

        _waveIcon = this.Q<VisualElement>("wave-icon");

        RegisterCallback<AttachToPanelEvent>(ev => _reporter.OnStateUpdated += UpdateVisuals);
        RegisterCallback<DetachFromPanelEvent>(ev => Dispose());

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (_reporter == null) return;

        if (_reporter.IsAvailable)
        {
            _waveIcon.RemoveFromClassList("wave-inactive");
            _waveIcon.AddToClassList("wave-active");
            _waveIcon.tooltip = "Correnti Oceaniche disponibili!";
        }
        else
        {
            _waveIcon.RemoveFromClassList("wave-active");
            _waveIcon.AddToClassList("wave-inactive");
            _waveIcon.tooltip = "Correnti Oceaniche già usate questo turno.";
        }
    }

    public void Dispose() => _reporter.OnStateUpdated -= UpdateVisuals;
}
