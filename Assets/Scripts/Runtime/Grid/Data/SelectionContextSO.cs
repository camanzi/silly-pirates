using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Grid/Selection Context")]
public class SelectionContextSO : ScriptableObject, ICombatSessionResettable
{
    public List<ITargettable> CurrentTargets => _currentTargets;

    public IInteractableElement CurrentCaster;
    private List<ITargettable> _currentTargets = new();

    [Header("Dependencies")]
    [SerializeField] private VoidEventChannel _onSelectionCtxChangeEventChannel;

    public bool IsElementSelected(IInteractableElement element)
    {
        return CurrentCaster == element || (element is ITargettable targettable && _currentTargets.Contains(targettable));
    }

    public void ClearCtx()
    {
        CurrentCaster = null;
        ClearTargets();
    }

    // Senza questo, CurrentCaster e i target restano puntatori a IInteractableElement distrutti
    // dallo scarico della scena precedente.
    public void ResetForNewCombat() => ClearCtx();

    public void ClearTargets()
    {
        _currentTargets.Clear();
        _onSelectionCtxChangeEventChannel.RaiseEvent();
    }
}
