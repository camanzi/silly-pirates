using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Grid/Selection Context")]
public class SelectionContextSO : ScriptableObject
{
    public IInteractableElement currentCaster;
    public List<IInteractableElement> currentTargets = new();

    [Header("Dependencies")]
    [SerializeField] private VoidEventChannel _onSelectionCtxChangeEventChannel;

    public bool IsElementSelected(IInteractableElement element)
    {
        return currentCaster == element || currentTargets.Contains(element);
    }

    public void ClearCtx()
    {
        currentCaster = null;
        currentTargets.Clear();
        _onSelectionCtxChangeEventChannel.RaiseEvent();
    }
}
