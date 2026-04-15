using UnityEngine;
using UnityEngine.UIElements;
using PrimeTween;

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

    private bool _isVisible = false;
    private Tween _visibilityTween;

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
            transform.LookAt(transform.position + _mainCamera.transform.rotation * Vector3.forward,
                             _mainCamera.transform.rotation * Vector3.up);
        }
    }

    public void ToggleVisibility(bool show)
    {
        if (show == _isVisible) return;
        _isVisible = show;

        _visibilityTween.Stop();

        if (show)
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
            }).OnComplete(() => _container.style.display = DisplayStyle.None); 
        }
    }

    public void BuildMenu()
    {
        _container.Clear();
        float delay = 0f;

        foreach (InteractionActionSO action in _interactionSet.AvailableActions)
        {
            if (CheckIfActionIsAllowed(action)) 
            {
                InteractionButton btn = new InteractionButton();
                btn.SetData(action, _bindedMenuElement, _currentTurnState.ActiveAgent, CheckIfActionIsEnabled(action));
                
                btn.style.scale = new StyleScale(new Scale(Vector3.zero));
                _container.Add(btn);

                Tween.Custom(0f, 1f, duration: 0.3f, startDelay: delay, ease: Ease.OutBack, onValueChange: newVal => {
                    btn.style.scale = new StyleScale(new Scale(new Vector3(newVal, newVal, 1f)));
                });

                delay += 0.1f;
            }
        }
    }

    private bool CheckIfActionIsAllowed(InteractionActionSO action) => true;
    private bool CheckIfActionIsEnabled(InteractionActionSO action) => true;
}
