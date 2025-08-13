using UnityEngine;
using Verse;

namespace LabelsOnFloor
{
    public class LabelsOnFloorSettings : ModSettings
    {
        // Settings fields
        public bool enabled = true;
        public Color defaultLabelColor = Color.white;
        public int opacity = 30;
        public bool showRoomLabels = true;
        public bool showZoneLabels = true;
        public bool showGrowingZoneLabels = true;
        public bool showStockpileZoneLabels = true;
        public float maxFontScale = 1f;
        public float minFontScale = 0.2f;
        public CameraZoomRange maxAllowedZoom = CameraZoomRange.Furthest;
        
        public override void ExposeData()
        {
            Scribe_Values.Look(ref enabled, "enabled", true);
            Scribe_Values.Look(ref defaultLabelColor, "defaultLabelColor", Color.white);
            Scribe_Values.Look(ref opacity, "opacity", 30);
            Scribe_Values.Look(ref showRoomLabels, "showRoomLabels", true);
            Scribe_Values.Look(ref showZoneLabels, "showZoneLabels", true);
            Scribe_Values.Look(ref showGrowingZoneLabels, "showGrowingZoneLabels", true);
            Scribe_Values.Look(ref showStockpileZoneLabels, "showStockpileZoneLabels", true);
            Scribe_Values.Look(ref maxFontScale, "maxFontScale", 1f);
            Scribe_Values.Look(ref minFontScale, "minFontScale", 0.2f);
            Scribe_Values.Look(ref maxAllowedZoom, "maxAllowedZoom", CameraZoomRange.Furthest);
            base.ExposeData();
        }
        
        // Helper methods for validation
        public void ValidateSettings()
        {
            opacity = Mathf.Clamp(opacity, 1, 100);
            maxFontScale = Mathf.Clamp(maxFontScale, 0.1f, 5.0f);
            minFontScale = Mathf.Clamp(minFontScale, 0.1f, 1.0f);
        }
    }
}