using UnityEngine;
using UnityEngine.UIElements;
using PrimeTween;
using System.Collections.Generic;

public class InteractionMenuController : MonoBehaviour
{
    [Header("Menu settings")]
    [SerializeField] private InteractionSetSO _interactionSet;
    [SerializeField] private UIDocument _uiDocument;
    
    [Header("Dependences")]
    // FIXME Later, sarebbe meglio l'interfaccia, peró non é serializzabile di default :/
    // Dove sei Thor editor....
    [SerializeField] private InteractableGridElement _bindedMenuElement;
    [SerializeField] private TurnStateSO _currentTurnState;

    private VisualElement _container;
    private Camera _mainCamera;
    private Dictionary<InteractionActionSO, InteractionButton> _activeButtons = new();
    private bool _isVisible = false;
    private Tween _visibilityTween;
    private bool _isRequested = false;
    private bool _isAllowedByState = true;

    void OnEnable()
    {
        _mainCamera = Camera.main;
        var root = _uiDocument.rootVisualElement;
        _container = root.Q<VisualElement>("container");
        
        if (_container != null)
        {
            _container.style.opacity = 0f;
            _container.style.display = DisplayStyle.None;
        }
    }

    void LateUpdate()
    {
        if (_mainCamera != null)
        {
            transform.LookAt(transform.position + _mainCamera.transform.rotation * Vector3.forward, _mainCamera.transform.rotation * Vector3.up);
        }
    }

    public void SetStatePermission(bool isAllowed)
    {
        _isAllowedByState = isAllowed;
        ApplyVisibility();
    }

    public void ToggleRequested(bool show)
    {
        _isRequested = show;
        ApplyVisibility();
    }

    private void ApplyVisibility()
    {
        bool finalVisibility = _isRequested && _isAllowedByState;

        if (finalVisibility == _isVisible) return;
        
        _visibilityTween.Stop();
        _isVisible = finalVisibility;

        if (finalVisibility)
        {
            BuildMenu();
            _container.style.display = DisplayStyle.Flex;
            _visibilityTween = Tween.Custom(0f, 1f, duration: .25f, ease: Ease.OutQuad, onValueChange: newVal => {
                _container.style.opacity = new StyleFloat(newVal);
            });
        }
        else
        {
            _visibilityTween = Tween.Custom(_container.style.opacity.value, 0f, duration: .25f, ease: Ease.OutQuad, onValueChange: newVal => {
                _container.style.opacity = new StyleFloat(newVal);
            }).OnComplete(() => {
                _activeButtons.Clear();
                _container.style.display = DisplayStyle.None;
            });
        }
    }

    public void BuildMenu()
    {
        _container.Clear();
        float delay = 0f;

        foreach (InteractionActionSO action in _interactionSet.AvailableActions)
        {
            if (!CheckIfActionIsAllowed(action)) continue;

            if (!_activeButtons.ContainsKey(action))
            {
                CreateInteractionButton(action, delay);
                delay += 0.1f;
            }
        }
    }

    public void RefreshMenu()
    {
        List<InteractionActionSO> available = _interactionSet.AvailableActions;
        float delay = 0f;

        List<InteractionActionSO> toRemove = new();
        foreach (KeyValuePair<InteractionActionSO, InteractionButton> pair in _activeButtons)
        {
            if (!available.Contains(pair.Key) || !CheckIfActionIsAllowed(pair.Key))
            {
                toRemove.Add(pair.Key);
            }
        }

        foreach (InteractionActionSO action in toRemove)
        {
            InteractionButton btn = _activeButtons[action];
            _activeButtons.Remove(action);
            
            Tween.Custom(1f, 0f, duration: 0.2f, onValueChange: v => {
                btn.style.scale = new StyleScale(new Scale(new Vector3(v, v, 1f)));
            }).OnComplete(() => _container.Remove(btn));
        }

        foreach (InteractionActionSO action in available)
        {
            if (!CheckIfActionIsAllowed(action)) continue;

            if (!_activeButtons.ContainsKey(action))
            {
                CreateInteractionButton(action, delay);
                delay += 0.1f;
            }
        }
    }

    private void CreateInteractionButton(InteractionActionSO action, float delay)
    {
        InteractionButton btn = new InteractionButton();
        btn.SetData(action, _bindedMenuElement, _currentTurnState.ActiveAgent, RefreshMenu);
        
        btn.style.scale = new StyleScale(new Scale(Vector3.zero));
        _container.Add(btn);
        _activeButtons.Add(action, btn);

        Tween.Custom(0f, 1f, duration: 0.3f, startDelay: delay, ease: Ease.OutBack, onValueChange: v => {
            btn.style.scale = new StyleScale(new Scale(new Vector3(v, v, 1f)));
        });
    }

    private bool CheckIfActionIsAllowed(InteractionActionSO action) => action.CanExecute(_bindedMenuElement, _currentTurnState.ActiveAgent);
}
