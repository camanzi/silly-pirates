using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ShootingEquipment))]
public class ShootingEquipmentEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var equipment = (ShootingEquipment)target;
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
}
