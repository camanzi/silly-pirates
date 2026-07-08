public static class InteractableExtensions 
{
    public static void HandlePointerEnter(this IInteractableElement element) 
    {
        element.OutlinerHelper.AddToOutline();
    }

    public static void HandlePointerClick(this IInteractableElement element) 
    {
        element.ClickChannel.RaiseEvent(element);
    }

    public static void HandlePointerExit(this IInteractableElement element)
    {
        bool isSelectedTarget = element.SelectionContext != null
            && element is ITargettable targettable
            && element.SelectionContext.CurrentTargets.Contains(targettable);

        if (!isSelectedTarget)
        {
            element.OutlinerHelper.RemoveFromOutline();
        }
    }
}