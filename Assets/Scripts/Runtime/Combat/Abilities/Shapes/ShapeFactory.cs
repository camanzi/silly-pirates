using System.Collections.Generic;
using UnityEngine;

public static class ShapeFactory {
    private static readonly Dictionary<ShapeType, IAreaShape> _shapes = new() {
        { ShapeType.Circle, new CircleShape() },
        { ShapeType.Line, new LineShape() }
    };

    public static IAreaShape GetShape(ShapeType type) {
        if (_shapes.TryGetValue(type, out var shape)) return shape;
        return null;
    }
}
