using UnityEngine;
using UnityEngine.UIElements;
using PrimeTween;

public class InteractionMenuController : MonoBehaviour
{
    [SerializeField] private InteractionSetSO interactionSet;
    [SerializeField] private UIDocument uiDocument;
    
    private VisualElement _container;
    private Camera _mainCamera;

    void OnEnable()
    {
        _mainCamera = Camera.main;
        var root = uiDocument.rootVisualElement;
        _container = root.Q<VisualElement>("container");
        
        BuildMenu();
    }

    void LateUpdate()
    {
        if (_mainCamera != null)
        {
            transform.LookAt(transform.position + _mainCamera.transform.rotation * Vector3.forward,
                             _mainCamera.transform.rotation * Vector3.up);
        }
    }

    public void BuildMenu()
    {
        _container.Clear();
        float delay = 0f;

        foreach (InteractionActionSO action in interactionSet.AvailableActions)
        {
            if (CheckIfActionIsAllowed(action)) 
            {
                InteractionButton btn = new InteractionButton();
                btn.SetData(action, CheckIfActionIsEnabled(action));
                
                btn.style.scale = new StyleScale(Vector3.zero);
                _container.Add(btn);

                Tween.Custom(Vector2.zero, Vector2.one, duration: 0.3f, startDelay: 0, endDelay: delay, ease: Ease.OutBack, onValueChange: newVal => {
                    btn.style.scale = new StyleScale(new Scale(new Vector3(newVal.x, newVal.y, 1f)));
                });

                delay += 0.05f;
            }
        }
    }

    private bool CheckIfActionIsAllowed(InteractionActionSO action) => true;
    private bool CheckIfActionIsEnabled(InteractionActionSO action) => true;
}
