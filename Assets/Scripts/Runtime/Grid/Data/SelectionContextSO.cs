using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Grid/Selection Context")]
public class SelectionContextSO : ScriptableObject
{
    public InteractableGridElement currentCaster;
    public List<GridElement> currentTargets = new();

    [Header("Dependencies")]
    [SerializeField] private VoidEventChannel _onSelectionCtxChangeEventChannel;

    public bool IsElementSelected(GridElement element)
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
