using UnityEngine;
using Verse;

namespace LabelsOnFloor
{
    public class LabelsOnFloorSettings : ModSettings
    {
        // Settings fields
        public bool enabled = true;
        public Color defaultLabelColor = Color.white;
        public int opacity = 18;  // Changed from 30 to match screenshot default
        public bool showRoomLabels = true;
        public bool showZoneLabels = true;  // Default should be true per feedback
        public bool showGrowingZoneLabels = true;
        public bool showStockpileZoneLabels = true;
        public float maxFontScale = 1f;
        public float minFontScale = 0.2f;
        public CameraZoomRange maxAllowedZoom = CameraZoomRange.Furthest;
        
        // Default values for reset
        public static class Defaults
        {
            public const bool Enabled = true;
            public static readonly Color DefaultLabelColor = Color.white;
            public const int Opacity = 18;
            public const bool ShowRoomLabels = true;
            public const bool ShowZoneLabels = true;
            public const bool ShowGrowingZoneLabels = true;
            public const bool ShowStockpileZoneLabels = true;
            public const float MaxFontScale = 1f;
            public const float MinFontScale = 0.2f;
            public const CameraZoomRange MaxAllowedZoom = CameraZoomRange.Furthest;
        }
        
        /// <summary>
        /// Reset all settings to default values
        /// </summary>
        public void ResetToDefaults()
        {
            enabled = Defaults.Enabled;
            defaultLabelColor = Defaults.DefaultLabelColor;
            opacity = Defaults.Opacity;
            showRoomLabels = Defaults.ShowRoomLabels;
            showZoneLabels = Defaults.ShowZoneLabels;
            showGrowingZoneLabels = Defaults.ShowGrowingZoneLabels;
            showStockpileZoneLabels = Defaults.ShowStockpileZoneLabels;
            maxFontScale = Defaults.MaxFontScale;
            minFontScale = Defaults.MinFontScale;
            maxAllowedZoom = Defaults.MaxAllowedZoom;
        }
        
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