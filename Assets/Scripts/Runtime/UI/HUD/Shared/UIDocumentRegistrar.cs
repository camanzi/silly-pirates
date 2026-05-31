using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class UIDocumentRegistrar : MonoBehaviour
{
    private UIDocument _doc;
    private void Awake()     => _doc = GetComponent<UIDocument>();
    private void OnEnable()  => UIPointerTracker.Register(_doc);
    private void OnDisable() => UIPointerTracker.Unregister(_doc);
}
