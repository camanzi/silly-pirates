using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

[CustomEditor(typeof(ShipEquipment), true)]
[CanEditMultipleObjects]
public class ShipEquipmentEditor : Editor
{
    private const string FootprintProperty = "_footprint";
    private const float PanelWidth = 200f;
    private const float PanelHeight = 46f;
    private const float MiniMapHeight = 170f;

    private static readonly Color OtherEquipmentColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    private const float OtherFillAlpha = 0.1f;
    private const float OtherOutlineAlpha = 0.4f;
    private static readonly Color DirectionButtonColor = new Color(1f, 1f, 1f, 0.8f);

    // The other equipment under the same Grid: drawn faint and fed to the overlap check.
    private readonly List<ShipEquipment> _siblings = new List<ShipEquipment>();
    private readonly HashSet<Vector3Int> _takenByOthers = new HashSet<Vector3Int>();
    private readonly List<Vector3Int> _cellBuffer = new List<Vector3Int>(HexFootprint.MaxLineCount);
    private readonly List<FootprintCell> _evaluation = new List<FootprintCell>(HexFootprint.MaxLineCount);

    // Last scene evaluation of the single selected target, shown by the Inspector mini-map and HelpBox.
    private readonly List<FootprintCell> _sceneEvaluation = new List<FootprintCell>(HexFootprint.MaxLineCount);
    private GridFootprint _sceneEvaluationFootprint;
    private bool _hasSceneEvaluation;
    private int _sceneEvaluationSignature;

    private readonly Vector3[] _corners = new Vector3[HexCellGeometry.CornerCount];
    private readonly Vector3[] _outline = new Vector3[HexCellGeometry.CornerCount + 1];

    // Delegates cached once so the per-event evaluation does not allocate closures.
    private Tilemap _floorTilemap;
    private System.Func<Vector3Int, bool> _isWalkable;
    private System.Func<Vector3Int, bool> _isTakenByOther;

    private Object[] _selection;

    private void OnEnable()
    {
        _isWalkable = cell => _floorTilemap == null || GridUtils.IsWalkable(_floorTilemap, cell);
        _isTakenByOther = _takenByOthers.Contains;
        _hasSceneEvaluation = false;
        // Unity warns when `targets` is read inside OnSceneGUI: snapshot it here. The editor is rebuilt
        // whenever the selection changes, so the snapshot never goes stale.
        _selection = targets;

        RefreshSiblings();
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
        Undo.undoRedoPerformed += OnUndoRedo;
    }

    private void OnDisable()
    {
        EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        Undo.undoRedoPerformed -= OnUndoRedo;
    }

    public override void OnInspectorGUI()
    {
        if (DrawDefaultInspector())
            SceneView.RepaintAll();

        if (!serializedObject.isEditingMultipleObjects)
            DrawFootprintPreview();

        var equipment = (ShipEquipment)target;
        var statsConfig = equipment.StatsConfig;
        var equipmentType = equipment.EquipmentType;

        EditorGUILayout.Space();

        if (statsConfig == null)
        {
            EditorGUILayout.HelpBox("Stats Config is not assigned.", MessageType.Warning);
            return;
        }

        bool mismatch = equipmentType == EquipmentType.Offensive && statsConfig is not OffensiveEquipmentStatsSO
                     || equipmentType == EquipmentType.Defensive && statsConfig is not DefensiveEquipmentStatsSO;

        if (mismatch)
        {
            string expected = equipmentType == EquipmentType.Offensive
                ? nameof(OffensiveEquipmentStatsSO)
                : nameof(DefensiveEquipmentStatsSO);
            EditorGUILayout.HelpBox(
                $"Stats Config type mismatch: {equipmentType} equipment requires a {expected}.",
                MessageType.Error);
        }
    }

    #region Inspector mini-map

    private void DrawFootprintPreview()
    {
        GridFootprint footprint = ((GridElement)target).Footprint;
        bool fresh = HasFreshSceneEvaluation(footprint);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Footprint Preview", EditorStyles.boldLabel);

        Rect rect = GUILayoutUtility.GetRect(10f, MiniMapHeight, GUILayout.ExpandWidth(true));
        if (FootprintPreviewGUI.Draw(rect, footprint, fresh ? _sceneEvaluation : null, out HexDirection clicked)
            && clicked != footprint.Direction)
        {
            ApplyFootprint(footprint.WithDirection(clicked), "Set Footprint Direction");
        }

        if (footprint.Shape != FootprintShape.Single)
            EditorGUILayout.LabelField("Click a cell around the anchor to set the direction.", EditorStyles.miniLabel);

        if (fresh)
            DrawPlacementProblems();
    }

    private void DrawPlacementProblems()
    {
        StringBuilder notWalkable = null;
        StringBuilder overlap = null;
        for (int i = 0; i < _sceneEvaluation.Count; i++)
        {
            FootprintCell cell = _sceneEvaluation[i];
            if (cell.Status == FootprintCellStatus.NotWalkable) Append(ref notWalkable, cell.Cell);
            else if (cell.Status == FootprintCellStatus.Overlap) Append(ref overlap, cell.Cell);
        }
        if (notWalkable == null && overlap == null) return;

        var message = new StringBuilder("Footprint placement problems (last Scene view evaluation):");
        if (notWalkable != null) message.Append("\nNot walkable: ").Append(notWalkable);
        if (overlap != null) message.Append("\nOverlaps other equipment: ").Append(overlap);
        EditorGUILayout.HelpBox(message.ToString(), MessageType.Warning);
    }

    private static void Append(ref StringBuilder builder, Vector3Int cell)
    {
        if (builder == null) builder = new StringBuilder();
        else builder.Append(", ");
        builder.Append('(').Append(cell.x).Append(", ").Append(cell.y).Append(')');
    }

    private bool HasFreshSceneEvaluation(GridFootprint footprint)
        => _hasSceneEvaluation
        && _sceneEvaluationFootprint.Shape == footprint.Shape
        && _sceneEvaluationFootprint.Count == footprint.Count
        && _sceneEvaluationFootprint.Direction == footprint.Direction;

    #endregion

    #region Scene view

    private void OnSceneGUI()
    {
        var equipment = target as ShipEquipment;
        if (equipment == null) return;

        // With several objects selected Unity calls OnSceneGUI once per target: the shared layers (other
        // equipment) are drawn once, and the interactive handles only exist for a single selection, since
        // serializedObject would write the same footprint to every target.
        bool isFirstTarget = _selection.Length == 0 || target == _selection[0];
        bool interactive = _selection.Length <= 1;
        bool repaint = Event.current.type == EventType.Repaint;

        if (!equipment.TryResolveFloorPreview(out GridLayout layout, out Vector3Int anchor))
        {
            if (interactive) ClearSceneEvaluation();
            return;
        }

        CollectOtherEquipment(equipment, layout, drawFaint: isFirstTarget && repaint);

        _floorTilemap = layout as Tilemap;
        GridFootprint footprint = equipment.Footprint;
        FootprintPlacement.Evaluate(anchor, footprint, _isWalkable, _isTakenByOther, _evaluation);
        if (interactive) StoreSceneEvaluation(footprint);

        HexCellGeometry.GetCornersWorld(layout, anchor, _corners);
        Vector3 anchorCentre = HexCellGeometry.CellCenterWorld(layout, anchor);
        Vector3 normal = PlaneNormal(_corners);
        float cellRadius = Vector3.Distance(anchorCentre, _corners[0]);

        if (repaint)
            DrawFootprint(layout, footprint, anchor, anchorCentre, normal, cellRadius);

        if (!interactive) return;

        if (footprint.Shape != FootprintShape.Single)
            DrawDirectionButtons(layout, anchor, footprint, normal, cellRadius);
        DrawControlPanel(anchorCentre, footprint);
    }

    private void CollectOtherEquipment(ShipEquipment self, GridLayout selfLayout, bool drawFaint)
    {
        _takenByOthers.Clear();
        for (int s = 0; s < _siblings.Count; s++)
        {
            ShipEquipment other = _siblings[s];
            if (other == null || other == self || !other.isActiveAndEnabled) continue;
            if (!other.TryResolveFloorPreview(out GridLayout otherLayout, out Vector3Int otherAnchor)) continue;
            // Only equipment standing on the same deck can overlap: cell coordinates of another grid
            // (another ship) mean nothing here.
            if (otherLayout != selfLayout) continue;

            HexFootprint.Resolve(otherAnchor, other.Footprint, _cellBuffer);
            bool draw = drawFaint && !IsTarget(other);
            for (int i = 0; i < _cellBuffer.Count; i++)
            {
                _takenByOthers.Add(_cellBuffer[i]);
                if (draw) DrawCell(otherLayout, _cellBuffer[i], OtherEquipmentColor, OtherFillAlpha, OtherOutlineAlpha);
            }
        }
    }

    private void DrawFootprint(GridLayout layout, GridFootprint footprint, Vector3Int anchor,
                               Vector3 anchorCentre, Vector3 normal, float cellRadius)
    {
        for (int i = 0; i < _evaluation.Count; i++)
        {
            Color color = FootprintPreviewGUI.StatusColor(_evaluation[i].Status, i == 0);
            DrawCell(layout, _evaluation[i].Cell, color, FootprintPreviewGUI.FillAlpha, 1f);
        }

        // Anchor marker, kept visible even when the anchor cell itself is invalid.
        Handles.color = FootprintPreviewGUI.AnchorColor;
        Handles.DrawSolidDisc(anchorCentre, normal, cellRadius * 0.18f);

        if (!footprint.HasDirection) return;

        Vector3 to = HexCellGeometry.CellCenterWorld(layout, HexCoordinates.Neighbour(anchor, footprint.Direction));
        Vector3 back = (anchorCentre - to) * 0.3f;
        Vector3 side = Vector3.Cross(back, normal) * 0.5f;
        Handles.DrawAAPolyLine(4f, anchorCentre, to);
        Handles.DrawAAPolyLine(4f, to + back + side, to, to + back - side);
    }

    private void DrawDirectionButtons(GridLayout layout, Vector3Int anchor, GridFootprint footprint,
                                      Vector3 normal, float cellRadius)
    {
        Quaternion rotation = Quaternion.LookRotation(normal);
        float size = cellRadius * 0.3f;

        for (int i = 0; i < HexCoordinates.DirectionCount; i++)
        {
            var direction = (HexDirection)i;
            Vector3 position = HexCellGeometry.CellCenterWorld(layout, HexCoordinates.Neighbour(anchor, direction));
            Handles.color = direction == footprint.Direction ? FootprintPreviewGUI.AnchorColor : DirectionButtonColor;
            if (Handles.Button(position, rotation, size, size, Handles.CircleHandleCap) && direction != footprint.Direction)
                ApplyFootprint(footprint.WithDirection(direction), "Set Footprint Direction");
        }
    }

    private void DrawControlPanel(Vector3 anchorCentre, GridFootprint footprint)
    {
        int max = HexFootprint.ClampCount(footprint.Shape, int.MaxValue);
        bool resizable = max > 1;

        Handles.BeginGUI();
        Vector2 guiPoint = HandleUtility.WorldToGUIPoint(anchorCentre);
        var box = new Rect(guiPoint.x + 24f, guiPoint.y - PanelHeight * 0.5f, PanelWidth, PanelHeight);
        GUI.Box(box, GUIContent.none, EditorStyles.helpBox);

        var label = new Rect(box.x + 4f, box.y + 3f, box.width - 8f, 16f);
        GUI.Label(label, $"{footprint.Shape}: {footprint.Count}/{max} cells", EditorStyles.miniBoldLabel);

        float buttonWidth = (box.width - 8f - 6f) / 4f;
        var button = new Rect(box.x + 4f, box.y + 23f, buttonWidth, 18f);

        using (new EditorGUI.DisabledScope(!resizable || footprint.Count <= 1))
        {
            if (GUI.Button(button, new GUIContent("-", "Remove a cell"), EditorStyles.miniButtonLeft))
                ApplyFootprint(footprint.WithCount(footprint.Count - 1), "Change Footprint Count");
        }
        button.x += buttonWidth;
        using (new EditorGUI.DisabledScope(!resizable || footprint.Count >= max))
        {
            if (GUI.Button(button, new GUIContent("+", "Add a cell"), EditorStyles.miniButtonRight))
                ApplyFootprint(footprint.WithCount(footprint.Count + 1), "Change Footprint Count");
        }
        button.x += buttonWidth + 6f;
        using (new EditorGUI.DisabledScope(!footprint.HasDirection))
        {
            if (GUI.Button(button, new GUIContent("CCW", "Rotate 60 degrees counter-clockwise"), EditorStyles.miniButtonLeft))
                ApplyFootprint(footprint.Rotated(1), "Rotate Footprint");
            button.x += buttonWidth;
            if (GUI.Button(button, new GUIContent("CW", "Rotate 60 degrees clockwise"), EditorStyles.miniButtonRight))
                ApplyFootprint(footprint.Rotated(-1), "Rotate Footprint");
        }

        // A click on the panel background (or a disabled button) must not reach the scene and change the selection.
        Event evt = Event.current;
        if (evt.type == EventType.MouseDown && box.Contains(evt.mousePosition))
            evt.Use();
        Handles.EndGUI();
    }

    private void DrawCell(GridLayout layout, Vector3Int cell, Color color, float fillAlpha, float outlineAlpha)
    {
        HexCellGeometry.GetCornersWorld(layout, cell, _corners);
        for (int i = 0; i < HexCellGeometry.CornerCount; i++) _outline[i] = _corners[i];
        _outline[HexCellGeometry.CornerCount] = _corners[0];

        Handles.color = new Color(color.r, color.g, color.b, fillAlpha);
        Handles.DrawAAConvexPolygon(_corners);
        Handles.color = new Color(color.r, color.g, color.b, outlineAlpha);
        Handles.DrawAAPolyLine(2f, _outline);
    }

    private static Vector3 PlaneNormal(Vector3[] corners)
    {
        Vector3 normal = Vector3.Cross(corners[1] - corners[0], corners[2] - corners[0]);
        return normal.sqrMagnitude > 0f ? normal.normalized : Vector3.up;
    }

    private bool IsTarget(Object candidate)
    {
        for (int i = 0; i < _selection.Length; i++)
            if (_selection[i] == candidate) return true;
        return false;
    }

    #endregion

    #region State

    /// <summary>
    /// Writes the footprint through a SerializedObject, so the change is undoable and recorded as a prefab
    /// override on instances. Only called with a single target.
    /// A temporary SerializedObject on <c>target</c> is used instead of the editor's own: this also runs from
    /// OnSceneGUI, where Unity forbids touching <c>serializedObject</c>. The Inspector picks the change up on
    /// its next serializedObject.Update().
    /// </summary>
    private void ApplyFootprint(GridFootprint footprint, string undoName)
    {
        using var so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(FootprintProperty);
        if (property == null) return;

        property.FindPropertyRelative("_shape").intValue = (int)footprint.Shape;
        property.FindPropertyRelative("_count").intValue = footprint.Count;
        property.FindPropertyRelative("_direction").intValue = (int)footprint.Direction;
        if (so.ApplyModifiedProperties())
            Undo.SetCurrentGroupName(undoName);

        SceneView.RepaintAll();
        Repaint();
    }

    private void StoreSceneEvaluation(GridFootprint footprint)
    {
        int signature = 17;
        for (int i = 0; i < _evaluation.Count; i++)
            signature = signature * 31 + _evaluation[i].Cell.GetHashCode() * 7 + (int)_evaluation[i].Status;

        _sceneEvaluation.Clear();
        _sceneEvaluation.AddRange(_evaluation);
        _sceneEvaluationFootprint = footprint;

        if (!_hasSceneEvaluation || signature != _sceneEvaluationSignature)
        {
            _hasSceneEvaluation = true;
            _sceneEvaluationSignature = signature;
            Repaint();
        }
    }

    private void ClearSceneEvaluation()
    {
        if (!_hasSceneEvaluation) return;
        _hasSceneEvaluation = false;
        _sceneEvaluation.Clear();
        Repaint();
    }

    private void RefreshSiblings()
    {
        _siblings.Clear();
        var self = target as ShipEquipment;
        if (self == null) return;

        // Every equipment in the same stage (the scene, or the prefab open in Prefab Mode). The hierarchy is
        // not assumed: on the ship the Grid is a sibling of the equipment, not a parent. Which ones really
        // share the deck is decided per evaluation, by comparing the resolved floor layout.
        _siblings.AddRange(StageUtility.GetStage(self.gameObject).FindComponentsOfType<ShipEquipment>());
    }

    private void OnHierarchyChanged()
    {
        RefreshSiblings();
        SceneView.RepaintAll();
    }

    private void OnUndoRedo()
    {
        SceneView.RepaintAll();
        Repaint();
    }

    #endregion
}
