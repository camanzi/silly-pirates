using UnityEngine;
using UnityEngine.UIElements;
using PrimeTween;
using System.Collections.Generic;
using System.Linq;

public class InteractionMenuController : WorldSpaceContainer
{
    [Header("Menu settings")]
    [SerializeField] private InteractionSetSO _interactionSet;
    
    [Header("Dependences")]
    // FIXME Later, sarebbe meglio l'interfaccia, peró non é serializzabile di default :/
    // Dove sei Odin inspector editor....
    [SerializeField] private InteractableGridElement _bindedMenuElement;
    [SerializeField] private TurnStateSO _currentTurnState;
    [SerializeField] private IntEventChannel _abilityHoverChannel;

    private Dictionary<InteractionActionSO, InteractionButton> _activeButtons = new();

    protected override void OnEnable()
    {
        base.OnEnable();

        if (Container != null)
        {
            Container.style.opacity = 0f;
            Container.style.display = DisplayStyle.None;
        }
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();
    }

    protected override void ShowUI()
    {
        base.ShowUI();
        Container.style.display = DisplayStyle.Flex;
        BuildMenu();
    }

    public void BuildMenu()
    {
        Container.Clear();
        _activeButtons.Clear();
        float delay = 0f;

        foreach (InteractionActionSO action in _interactionSet.AvailableActions)
        {
            if (!ShouldShowAction(action)) continue;

            if (!_activeButtons.ContainsKey(action))
            {
                CreateInteractionButton(action, delay);
                delay += 0.1f;
            }
        }

        UpdateEnabledStates();
    }

    protected override void OnCompleteHide()
    {
        base.OnCompleteHide();
        _activeButtons.Clear();
        _abilityHoverChannel?.RaiseEvent(0);
        (_bindedMenuElement as IAwakable)?.OnAwakeningHoverPreview?.Invoke(0);
    }

    protected override void OnCompleteShow()
    {
        base.OnCompleteShow();
    }

    protected override void RefreshUI()
    {
        base.RefreshUI();
        List<InteractionActionSO> available = _interactionSet.AvailableActions;
        float delay = 0f;

        var toRemove = new List<InteractionActionSO>();
        foreach (KeyValuePair<InteractionActionSO, InteractionButton> pair in _activeButtons)
        {
            if (!available.Contains(pair.Key) || !ShouldShowAction(pair.Key))
                toRemove.Add(pair.Key);
        }

        var toAdd = new List<InteractionActionSO>();
        foreach (InteractionActionSO action in available)
        {
            if (ShouldShowAction(action) && !_activeButtons.ContainsKey(action))
                toAdd.Add(action);
        }

        // In-place swap: se una nuova azione dichiara di sostituire una che sta per essere rimossa,
        // aggiorna il bottone esistente invece di rimuoverlo e crearne uno nuovo.
        foreach (InteractionActionSO newAction in toAdd.ToList())
        {
            if (newAction is not IInPlaceSwappable swappable) continue;
            if (!toRemove.Contains(swappable.BaseAction)) continue;

            InteractionButton btn = _activeButtons[swappable.BaseAction];
            _activeButtons.Remove(swappable.BaseAction);
            _activeButtons[newAction] = btn;
            btn.UpdateData(newAction);

            toRemove.Remove(swappable.BaseAction);
            toAdd.Remove(newAction);
        }

        foreach (InteractionActionSO action in toRemove)
        {
            InteractionButton btn = _activeButtons[action];
            _activeButtons.Remove(action);

            Tween.Custom(1f, 0f, duration: 0.2f, onValueChange: v => {
                btn.style.scale = new StyleScale(new Scale(new Vector3(v, v, 1f)));
            }).OnComplete(() => Container.Remove(btn));
        }

        foreach (InteractionActionSO action in toAdd)
        {
            CreateInteractionButton(action, delay);
            delay += 0.1f;
        }

        UpdateEnabledStates();
    }

    private void CreateInteractionButton(InteractionActionSO action, float delay)
    {
        InteractionButton btn = new InteractionButton();
        btn.SetData(action, _bindedMenuElement, _currentTurnState.ActiveAgent, RefreshUI, _abilityHoverChannel);
        
        btn.style.scale = new StyleScale(new Scale(Vector3.zero));
        Container.Add(btn);
        _activeButtons.Add(action, btn);

        Tween.Custom(0f, 1f, duration: 0.3f, startDelay: delay, ease: Ease.OutBack, onValueChange: v => {
            btn.style.scale = new StyleScale(new Scale(new Vector3(v, v, 1f)));
        });
    }

    private bool ShouldShowAction(InteractionActionSO action)
        => action.CanShow(_bindedMenuElement, _currentTurnState.ActiveAgent);

    private void UpdateEnabledStates()
    {
        foreach (var pair in _activeButtons)
            pair.Value.SetEnabled(pair.Key.CanExecute(_bindedMenuElement, _currentTurnState.ActiveAgent));
    }
}
