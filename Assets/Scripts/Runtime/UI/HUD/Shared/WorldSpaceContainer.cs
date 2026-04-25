using UnityEngine;
using UnityEngine.UIElements;

public class WorldSpaceContainer : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] protected UIDocument _uiDocument;
    protected VisualElement Container => _container;
    protected Camera MainCamera => _mainCamera; 
    private VisualElement _container;
    private Camera _mainCamera;
 
    protected virtual void OnEnable()
    {
        _mainCamera = Camera.main;
        var root = _uiDocument.rootVisualElement;
        _container = root.Q<VisualElement>("container");

        root.style.position = Position.Absolute;
        root.pickingMode = PickingMode.Ignore;

        if (_container != null) _container.pickingMode = PickingMode.Position;
    }

    protected virtual void LateUpdate()
    {
       if (_mainCamera == null || _container == null) return;
        
        Vector3 screenPos = _mainCamera.WorldToScreenPoint(transform.position);
        
        if (screenPos.z > 0)
        {
            _container.style.display = DisplayStyle.Flex;
            
            Vector2 panelPos = RuntimePanelUtils.CameraTransformWorldToPanel(
                _container.panel, transform.position, _mainCamera);

            _container.style.left = panelPos.x;
            _container.style.top = panelPos.y;
        }
        else
        {
            _container.style.display = DisplayStyle.None;
        }
    }
}
