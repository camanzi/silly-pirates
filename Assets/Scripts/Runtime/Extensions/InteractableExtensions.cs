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
        if (element.SelectionContext == null || !element.SelectionContext.IsElementSelected(element))
        {
            element.OutlinerHelper.RemoveFromOutline();
        }
    }
}