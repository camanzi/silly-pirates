using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Grid/Selection Context")]
public class SelectionContextSO : ScriptableObject
{
    public List<ITargettable> CurrentTargets => _currentTargets;

    public IInteractableElement currentCaster;
    private List<ITargettable> _currentTargets = new();

    [Header("Dependencies")]
    [SerializeField] private VoidEventChannel _onSelectionCtxChangeEventChannel;

    public bool IsElementSelected(IInteractableElement element)
    {
        return currentCaster == element || (element is ITargettable targettable && _currentTargets.Contains(targettable));
    }

    public void ClearCtx()
    {
        currentCaster = null;
        _currentTargets.Clear();
        _onSelectionCtxChangeEventChannel.RaiseEvent();
    }
}
