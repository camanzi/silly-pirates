using PrimeTween;
using UnityEngine;
using UnityEngine.UIElements;

public class WorldSpaceContainer : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] protected UIDocument _uiDocument;
    [Tooltip("If true try to show the menu by default without requesting it by proximity")]
    [SerializeField] private bool _shownByDefault;
    [SerializeField] protected Vector3 _positionOffset;
    [SerializeField] protected PickingMode _pickingMode = PickingMode.Ignore;

    [Header("Anchors")]
    [SerializeField] private MainCameraAnchorSO _mainCameraAnchor;

    protected VisualElement Container => _container;
    protected Camera MainCamera => _mainCamera; 
    private VisualElement _container;
    protected bool _isVisible = false;
    protected Tween _visibilityTween;
    protected bool _isRequested = false;
    protected bool _isAllowedByCombatState = true;
    protected bool _isAllowedByElementState = true;
    private Camera _mainCamera;
 
    protected virtual void OnEnable()
    {
        if (_mainCameraAnchor != null)
        {
            _mainCamera = _mainCameraAnchor.Value;
            _mainCameraAnchor.OnValueChanged += HandleCameraChanged;
        }
        else
        {
            Debug.LogError($"{GetType().Name}: nessun {nameof(MainCameraAnchorSO)} assegnato.", this);
        }

        UpdateUIPosition();
        UIPointerTracker.Register(_uiDocument);
    }

    protected virtual void OnDisable()
    {
        if (_mainCameraAnchor != null) _mainCameraAnchor.OnValueChanged -= HandleCameraChanged;
        UIPointerTracker.Unregister(_uiDocument);
    }

    private void HandleCameraChanged(Camera camera) => _mainCamera = camera;

    protected virtual void Awake()
    {
        _isRequested = _shownByDefault;
        var root = _uiDocument.rootVisualElement;
        _container = root.Q<VisualElement>("container");

        root.style.position = Position.Absolute;
        root.pickingMode = PickingMode.Ignore;

        if (_container != null)
        {
            _container.pickingMode = _pickingMode;
            _container.RegisterCallback<GeometryChangedEvent>(evt => {
                if (evt.oldRect.size != evt.newRect.size)
                    UpdateUIPosition();
            });
        }

        // Allinea subito _isVisible/opacity/display al gate corrente senza tween: al frame zero
        // _isVisible parte a false per default del campo, quindi un ApplyVisibility() normale
        // tenterebbe un fade-out da opacità 1 (lampeggio) invece di un semplice nascondere silenzioso.
        ApplyVisibilityImmediate();
    }

    // Use when combat state changes and the element should hide his UI
    public void SetCombatStatePermission(bool isAllowed)
    {
        _isAllowedByCombatState = isAllowed;
        ApplyVisibility();
    }

    // Use when element should hide his UI for some internal states
    public void SetElementStatePermission(bool isAllowed)
    {
        _isAllowedByElementState = isAllowed;
        ApplyVisibility();
    }
    
    public void ToggleRequested(bool show)
    {
        _isRequested = show;
        ApplyVisibility();
    }


    // Gate in AND dei tre permessi: richiesta esplicita (proximity/menu), stato di combattimento
    // (es. intro), stato interno dell'elemento (es. morte). Condiviso tra ApplyVisibility() e
    // ApplyVisibilityImmediate() per non rischiare che le due formule divergano nel tempo.
    private bool FinalVisibility => _isRequested && _isAllowedByCombatState && _isAllowedByElementState;

    protected void ApplyVisibility()
    {
        if (Container == null) return;

        bool finalVisibility = FinalVisibility;

        if (finalVisibility == _isVisible)
        {
            if (finalVisibility)
                RefreshUI();
            return;
        }

        _visibilityTween.Stop();
        _isVisible = finalVisibility;

        if (finalVisibility)
        {
            // Il container potrebbe essere stato messo a display:None da un OnCompleteHide precedente:
            // va ripristinato prima di animare l'opacità, altrimenti il tween scrive su un elemento
            // che il layout non renderizza comunque.
            Container.style.display = DisplayStyle.Flex;
            ShowUI();
            float startOpacity = Container.style.opacity.value;
            _visibilityTween = Tween.Custom(startOpacity, 1f, duration: .25f, ease: Ease.OutQuad, onValueChange: newVal => {
                Container.style.opacity = new StyleFloat(newVal);
            }).OnComplete(() => OnCompleteShow());
        }
        else
        {
            _visibilityTween = Tween.Custom(Container.style.opacity.value, 0f, duration: .25f, ease: Ease.OutQuad, onValueChange: newVal => {
                Container.style.opacity = new StyleFloat(newVal);
            }).OnComplete(() => OnCompleteHide());
        }
    }

    /// <summary>
    /// Applica il gate corrente senza tween, scrivendo opacità e display in un solo frame.
    /// Va usata in Awake (prima frame utile, un fade da opacità 1 lampeggerebbe) e ogni volta che
    /// un permesso viene deciso prima che il container sia mai stato mostrato una volta.
    /// </summary>
    protected void ApplyVisibilityImmediate()
    {
        if (Container == null) return;

        _visibilityTween.Stop();
        bool finalVisibility = FinalVisibility;
        _isVisible = finalVisibility;

        Container.style.opacity = finalVisibility ? 1f : 0f;
        Container.style.display = finalVisibility ? DisplayStyle.Flex : DisplayStyle.None;
    }

    protected virtual void ShowUI() {}

    protected virtual void RefreshUI() {}

    protected virtual void OnCompleteHide()
    {
        if (!_isVisible)
            Container.style.display = DisplayStyle.None;
    }

    protected virtual void OnCompleteShow()
    {
        
    }

    protected virtual void LateUpdate()
    {
        UpdateUIPosition();
    }

    protected void UpdateUIPosition()
    {
        if (_mainCamera == null || _container == null || _container.style.display == DisplayStyle.None) 
            return;

        Vector3 screenPos = _mainCamera.WorldToScreenPoint(transform.position + _positionOffset);

        if (screenPos.z > 0)
        {
            Vector2 panelPos = RuntimePanelUtils.CameraTransformWorldToPanel(
                _container.panel, transform.position + _positionOffset, _mainCamera);

            _container.style.left = panelPos.x;
            _container.style.top = panelPos.y;
        }
    }
}
