using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public static class UIPointerTracker
{
    private static readonly HashSet<UIDocument> _documents = new();

    public static void Register(UIDocument doc)   => _documents.Add(doc);
    public static void Unregister(UIDocument doc) => _documents.Remove(doc);

    public static bool IsPointerOverUI(Vector2 screenPos)
    {
        foreach (var doc in _documents)
        {
            if (doc == null) continue;
            var panel = doc.rootVisualElement?.panel;
            if (panel == null) continue;
            // Panel-space origin is top-left; screen-space origin is bottom-left
            var panelPos = new Vector2(screenPos.x, Screen.height - screenPos.y);
            if (panel.Pick(panelPos) != null)
                return true;
        }
        return false;
    }
}
