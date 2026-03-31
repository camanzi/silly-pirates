using PrimeTween;
using UnityEngine;

public class GridEquipment : InteractableGridElement, ITargettable
{
    [Header("Grid Equipment configuration")]
    public float damage;

    protected override void Awake()
    {
        base.Awake();
    }
}
