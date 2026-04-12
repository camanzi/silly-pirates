using UnityEngine;
using UnityEngine.EventSystems;

public interface IClickable
{
    public void OnHoverEnter();
    public void OnHoverExit();
    public void OnClick();
}
