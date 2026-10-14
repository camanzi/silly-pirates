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
    private const float PanelWidth = 200f;
    private const float PanelHeight = 46f;
    private const float MiniMapHeight = 170f;
    private const float RotationStep = 60f;

    private static readonly Color OtherEquipmentColor = new Color(0.85f, 0.85f, 0.85f, 1f);
    private const float OtherFillAlpha = 0.1f;
    private const float OtherOutlineAlpha = 0.4f;

    /// <summary>
    /// Where an element's footprint was last resolved, cached because the collider probe is far too costly to
    /// repeat on every IMGUI event for every equipment on the deck. Keyed on the element's
    /// <c>localToWorldMatrix</c> (moving or rotating it, or the ship, recomputes it); anything else (collider
    /// edits, child transforms, the floor) clears the whole cache through <see cref="ObjectChangeEvents"/>.
    /// </summary>
    private sealed class FootprintPreview
    {
        public bool IsCurrent;
        public Matrix4x4 Matrix;
        public bool Resolved;
        public GridLayout Layout;
        public Vector3Int Anchor;
        public readonly List<Vector3Int> Cells = new List<Vector3Int>(ColliderFootprintProbe.MaxCells);
    }

    private readonly Dictionary<GridElement, FootprintPreview> _previews = new Dictionary<GridElement, FootprintPreview>();

    // The other equipment under the same Grid: drawn faint and fed to the overlap check.
    private readonly List<ShipEquipment> _siblings = new List<ShipEquipment>();
    private readonly HashSet<Vector3Int> _takenByOthers = new HashSet<Vector3Int>();
    private readonly List<FootprintCell> _evaluation = new List<FootprintCell>(ColliderFootprintProbe.MaxCells);
    private readonly List<FootprintCell> _inspectorEvaluation = new List<FootprintCell>(ColliderFootprintProbe.MaxCells);

    // Last scene evaluation of the single selected target, shown by the Inspector mini-map and HelpBox.
    private readonly List<FootprintCell> _sceneEvaluation = new List<FootprintCell>(ColliderFootprintProbe.MaxCells);
    private Vector3Int _sceneEvaluationAnchor;
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
        ObjectChangeEvents.changesPublished += OnChangesPublished;
    }

    private void OnDisable()
    {
        EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        Undo.undoRedoPerformed -= OnUndoRedo;
        ObjectChangeEvents.changesPublished -= OnChangesPublished;
    }

    public override void OnInspectorGUI()
    {
        if (DrawDefaultInspector())
        {
            InvalidatePreviews();
            SceneView.RepaintAll();
        }

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
        var element = (GridElement)target;
        FootprintPreview preview = GetPreview(element);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Footprint Preview", EditorStyles.boldLabel);
        Rect rect = GUILayoutUtility.GetRect(10f, MiniMapHeight, GUILayout.ExpandWidth(true));

        if (!preview.Resolved)
        {
            FootprintPreviewGUI.Draw(rect, null, default);
            return;
        }

        bool fresh = HasFreshSceneEvaluation(preview);
        // Without a fresh Scene view evaluation the cells are still known, only their status is not.
        if (!fresh) FootprintPlacement.Evaluate(preview.Cells, null, null, _inspectorEvaluation);
        FootprintPreviewGUI.Draw(rect, fresh ? _sceneEvaluation : _inspectorEvaluation, preview.Anchor);

        EditorGUILayout.LabelField(CellCountLabel(element.Footprint, preview.Cells.Count), EditorStyles.miniLabel);

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

    /// <summary>True when the last Scene view evaluation was made for exactly the cells the element covers now.</summary>
    private bool HasFreshSceneEvaluation(FootprintPreview preview)
    {
        if (!_hasSceneEvaluation || _sceneEvaluationAnchor != preview.Anchor) return false;
        if (_sceneEvaluation.Count != preview.Cells.Count) return false;
        for (int i = 0; i < _sceneEvaluation.Count; i++)
            if (_sceneEvaluation[i].Cell != preview.Cells[i]) return false;
        return true;
    }

    private static string CellCountLabel(GridFootprint footprint, int count)
        => $"{footprint.Source}: {count} {(count == 1 ? "cell" : "cells")}";

    #endregion

    #region Scene view

    private void OnSceneGUI()
    {
        var equipment = target as ShipEquipment;
        if (equipment == null) return;

        // With several objects selected Unity calls OnSceneGUI once per target: the shared layers (other
        // equipment) are drawn once, and the interactive handles only exist for a single selection.
        bool isFirstTarget = _selection.Length == 0 || target == _selection[0];
        bool interactive = _selection.Length <= 1;
        bool repaint = Event.current.type == EventType.Repaint;

        FootprintPreview self = GetPreview(equipment);
        if (!self.Resolved)
        {
            if (interactive) ClearSceneEvaluation();
            return;
        }

        GridLayout layout = self.Layout;
        Vector3Int anchor = self.Anchor;
        CollectOtherEquipment(equipment, layout, drawFaint: isFirstTarget && repaint);

        _floorTilemap = layout as Tilemap;
        FootprintPlacement.Evaluate(self.Cells, _isWalkable, _isTakenByOther, _evaluation);
        if (interactive) StoreSceneEvaluation(anchor);

        HexCellGeometry.GetCornersWorld(layout, anchor, _corners);
        Vector3 anchorCentre = HexCellGeometry.CellCenterWorld(layout, anchor);
        Vector3 normal = HexCellGeometry.PlaneNormalWorld(layout, anchor);
        float cellRadius = Vector3.Distance(anchorCentre, _corners[0]);

        if (repaint)
            DrawFootprint(layout, anchorCentre, normal, cellRadius);

        if (!interactive) return;

        DrawControlPanel(equipment, anchorCentre, normal, self.Cells.Count);
    }

    private void CollectOtherEquipment(ShipEquipment self, GridLayout selfLayout, bool drawFaint)
    {
        _takenByOthers.Clear();
        for (int s = 0; s < _siblings.Count; s++)
        {
            ShipEquipment other = _siblings[s];
            if (other == null || other == self || !other.isActiveAndEnabled) continue;

            FootprintPreview preview = GetPreview(other);
            // Only equipment standing on the same deck can overlap: cell coordinates of another grid
            // (another ship) mean nothing here.
            if (!preview.Resolved || preview.Layout != selfLayout) continue;

            bool draw = drawFaint && !IsTarget(other);
            for (int i = 0; i < preview.Cells.Count; i++)
            {
                _takenByOthers.Add(preview.Cells[i]);
                if (draw) DrawCell(preview.Layout, preview.Cells[i], OtherEquipmentColor, OtherFillAlpha, OtherOutlineAlpha);
            }
        }
    }

    private void DrawFootprint(GridLayout layout, Vector3 anchorCentre, Vector3 normal, float cellRadius)
    {
        for (int i = 0; i < _evaluation.Count; i++)
        {
            Color color = FootprintPreviewGUI.StatusColor(_evaluation[i].Status, i == 0);
            DrawCell(layout, _evaluation[i].Cell, color, FootprintPreviewGUI.FillAlpha, 1f);
        }

        // Anchor marker, kept visible even when the anchor cell itself is invalid.
        Handles.color = FootprintPreviewGUI.AnchorColor;
        Handles.DrawSolidDisc(anchorCentre, normal, cellRadius * 0.18f);
    }

    private void DrawControlPanel(ShipEquipment equipment, Vector3 anchorCentre, Vector3 normal, int cellCount)
    {
        Transform transform = equipment.transform;
        Vector3 pivotOffset = InPlaneOffset(transform.position, anchorCentre, normal);
        bool centred = pivotOffset.sqrMagnitude < 1e-8f;

        Handles.BeginGUI();
        Vector2 guiPoint = HandleUtility.WorldToGUIPoint(anchorCentre);
        var box = new Rect(guiPoint.x + 24f, guiPoint.y - PanelHeight * 0.5f, PanelWidth, PanelHeight);
        GUI.Box(box, GUIContent.none, EditorStyles.helpBox);

        var label = new Rect(box.x + 4f, box.y + 3f, box.width - 8f, 16f);
        GUI.Label(label, CellCountLabel(equipment.Footprint, cellCount), EditorStyles.miniBoldLabel);

        float rotateWidth = (box.width - 8f - 6f) * 0.25f;
        var button = new Rect(box.x + 4f, box.y + 23f, rotateWidth, 18f);

        // Rotating the transform rotates the colliders, and with them a Collider footprint. Around the anchor
        // cell centre rather than the pivot, so the anchor stays put; "counter-clockwise" as seen from above.
        if (GUI.Button(button, new GUIContent("CCW", "Rotate 60 degrees counter-clockwise (seen from above) " +
                                                     "around the anchor cell centre"), EditorStyles.miniButtonLeft))
            RotateAroundAnchor(transform, anchorCentre, normal, -RotationStep);
        button.x += rotateWidth;
        if (GUI.Button(button, new GUIContent("CW", "Rotate 60 degrees clockwise (seen from above) " +
                                                    "around the anchor cell centre"), EditorStyles.miniButtonRight))
            RotateAroundAnchor(transform, anchorCentre, normal, RotationStep);
        button.x += rotateWidth + 6f;
        button.width = box.xMax - 4f - button.x;

        using (new EditorGUI.DisabledScope(centred))
        {
            if (GUI.Button(button, new GUIContent("Snap Pivot", "Move the pivot onto the anchor cell centre " +
                                                               "(its height above the deck is kept)"), EditorStyles.miniButton))
            {
                Undo.RecordObject(transform, "Snap Pivot To Cell Centre");
                transform.position -= pivotOffset;
                AfterTransformEdit();
            }
        }

        // A click on the panel background (or a disabled button) must not reach the scene and change the selection.
        Event evt = Event.current;
        if (evt.type == EventType.MouseDown && box.Contains(evt.mousePosition))
            evt.Use();
        Handles.EndGUI();
    }

    private void RotateAroundAnchor(Transform transform, Vector3 anchorCentre, Vector3 normal, float degrees)
    {
        // Recorded on the transform, so on a prefab instance the change lands as a regular override.
        Undo.RecordObject(transform, "Rotate Equipment");
        transform.RotateAround(anchorCentre, normal, degrees);
        AfterTransformEdit();
    }

    private void AfterTransformEdit()
    {
        InvalidatePreviews();
        SceneView.RepaintAll();
        Repaint();
    }

    /// <summary>The component of <c>point - planePoint</c> lying in the grid plane.</summary>
    private static Vector3 InPlaneOffset(Vector3 point, Vector3 planePoint, Vector3 normal)
    {
        Vector3 offset = point - planePoint;
        return offset - normal * Vector3.Dot(offset, normal);
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

    private bool IsTarget(Object candidate)
    {
        for (int i = 0; i < _selection.Length; i++)
            if (_selection[i] == candidate) return true;
        return false;
    }

    #endregion

    #region State

    /// <summary>The cached footprint of <paramref name="element"/>, recomputed when its transform changed.</summary>
    private FootprintPreview GetPreview(GridElement element)
    {
        if (!_previews.TryGetValue(element, out FootprintPreview preview))
        {
            preview = new FootprintPreview();
            _previews.Add(element, preview);
        }

        Matrix4x4 matrix = element.transform.localToWorldMatrix;
        if (!preview.IsCurrent || preview.Matrix != matrix)
        {
            preview.Resolved = element.TryResolveFootprintPreview(out preview.Layout, out preview.Anchor, preview.Cells);
            preview.Matrix = matrix;
            preview.IsCurrent = true;
        }
        return preview;
    }

    private void InvalidatePreviews()
    {
        foreach (FootprintPreview preview in _previews.Values)
            preview.IsCurrent = false;
    }

    private void StoreSceneEvaluation(Vector3Int anchor)
    {
        int signature = 17;
        for (int i = 0; i < _evaluation.Count; i++)
            signature = signature * 31 + _evaluation[i].Cell.GetHashCode() * 7 + (int)_evaluation[i].Status;

        _sceneEvaluation.Clear();
        _sceneEvaluation.AddRange(_evaluation);
        _sceneEvaluationAnchor = anchor;

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
        // Destroyed elements would otherwise stay in the cache as dead keys.
        _previews.Clear();
        RefreshSiblings();
        SceneView.RepaintAll();
    }

    private void OnChangesPublished(ref ObjectChangeEventStream stream)
    {
        // Collider sizes, child transforms, the floor tiles: none of them moves the element's matrix.
        InvalidatePreviews();
        SceneView.RepaintAll();
        Repaint();
    }

    private void OnUndoRedo()
    {
        InvalidatePreviews();
        SceneView.RepaintAll();
        Repaint();
    }

    #endregion
}
