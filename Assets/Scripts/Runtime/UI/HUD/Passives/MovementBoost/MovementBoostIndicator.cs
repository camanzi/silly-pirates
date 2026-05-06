using UnityEngine;
using UnityEngine.UIElements;

public class MovementBoostIndicator : VisualElement, IDisposableUI
{
    private VisualElement _shoeIcon;
    private IMovementBoostUIReporter _reporter;

    public MovementBoostIndicator(IMovementBoostUIReporter reporter, VisualTreeAsset uxml)
    {
        _reporter = reporter;
        uxml.CloneTree(this);

        style.height = Length.Percent(100);
        style.aspectRatio = 1;
        style.marginRight = 5;
        
        _shoeIcon = this.Q<VisualElement>("shoe-icon");
                
        RegisterCallback<AttachToPanelEvent>(ev => _reporter.OnStateUpdated += UpdateVisuals);
        RegisterCallback<DetachFromPanelEvent>(ev => Dispose());
        
        UpdateVisuals();
    }

    private void UpdateVisuals() 
    {
        if (_reporter == null) return;

        bool isActive = _reporter.CurrentStacks > 0;
        
        if (isActive)
        {
            _shoeIcon.RemoveFromClassList("shoe-inactive");
            _shoeIcon.AddToClassList("shoe-active");
            
            _shoeIcon.tooltip = $"Bonus Movimento!\nStack: {_reporter.CurrentStacks}\nTurni rimanenti: {_reporter.TurnsUntilDecay}";
        }
        else
        {
            _shoeIcon.RemoveFromClassList("shoe-active");
            _shoeIcon.AddToClassList("shoe-inactive");
            
            _shoeIcon.tooltip = $"Bonus Movimento {_reporter.CurrentStacks}";
        }
    }

    public void Dispose() => _reporter.OnStateUpdated -= UpdateVisuals;
}
