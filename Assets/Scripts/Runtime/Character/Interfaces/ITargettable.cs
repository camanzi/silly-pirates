using UnityEngine;
using UnityEngine.EventSystems;

public interface ITargettable: IClickable
{
    public Transform Transform { get; }

}
