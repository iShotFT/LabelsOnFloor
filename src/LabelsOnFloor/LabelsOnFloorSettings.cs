using UnityEngine;
using Verse;

namespace LabelsOnFloor
{
    public enum LabelVisibilityMode
    {
        Visible = 0,       // Normal rendering
        DrawOnTop = 1,     // Draw above everything
        Hidden = 2         // Not visible
    }
    
    public class LabelsOnFloorSettings : ModSettings
    {
        // Settings fields
        public bool enabled = true;  // Legacy - kept for compatibility
        public LabelVisibilityMode visibilityMode = LabelVisibilityMode.Visible;
        public string selectedFont = "Classic";  // Default to Classic font
        public Color defaultLabelColor = Color.white;
        public int opacity = 75;  // Changed to 75% for better visibility
        public bool showRoomLabels = true;
        public bool showZoneLabels = true;  // Default should be true per feedback
        public bool showGrowingZoneLabels = true;
        public bool showStockpileZoneLabels = true;
        public float maxFontScale = 1f;
        public float minFontScale = 0.2f;
        public CameraZoomRange maxAllowedZoom = CameraZoomRange.Furthest;
        
        // Mod Compatibility Settings
        public bool hideLabelsInScreenshots = false;
        
        // Default values for reset
        public static class Defaults
        {
            public const bool Enabled = true;
            public const string SelectedFont = "Classic";
            public static readonly Color DefaultLabelColor = Color.white;
            public const int Opacity = 75;
            public const bool ShowRoomLabels = true;
            public const bool ShowZoneLabels = true;
            public const bool ShowGrowingZoneLabels = true;
            public const bool ShowStockpileZoneLabels = true;
            public const float MaxFontScale = 1f;
            public const float MinFontScale = 0.2f;
            public const CameraZoomRange MaxAllowedZoom = CameraZoomRange.Furthest;
            public const bool HideLabelsInScreenshots = false;
        }
        
        /// <summary>
        /// Reset all settings to default values
        /// </summary>
        public void ResetToDefaults()
        {
            enabled = Defaults.Enabled;
            selectedFont = Defaults.SelectedFont;
            defaultLabelColor = Defaults.DefaultLabelColor;
            opacity = Defaults.Opacity;
            showRoomLabels = Defaults.ShowRoomLabels;
            showZoneLabels = Defaults.ShowZoneLabels;
            showGrowingZoneLabels = Defaults.ShowGrowingZoneLabels;
            showStockpileZoneLabels = Defaults.ShowStockpileZoneLabels;
            maxFontScale = Defaults.MaxFontScale;
            minFontScale = Defaults.MinFontScale;
            maxAllowedZoom = Defaults.MaxAllowedZoom;
            hideLabelsInScreenshots = Defaults.HideLabelsInScreenshots;
        }
        
        public override void ExposeData()
        {
            Scribe_Values.Look(ref enabled, "enabled", true);
            Scribe_Values.Look(ref visibilityMode, "visibilityMode", LabelVisibilityMode.Visible);
            Scribe_Values.Look(ref selectedFont, "selectedFont", "JetBrainsMono");
            Scribe_Values.Look(ref defaultLabelColor, "defaultLabelColor", Color.white);
            Scribe_Values.Look(ref opacity, "opacity", 30);
            Scribe_Values.Look(ref showRoomLabels, "showRoomLabels", true);
            Scribe_Values.Look(ref showZoneLabels, "showZoneLabels", true);
            Scribe_Values.Look(ref showGrowingZoneLabels, "showGrowingZoneLabels", true);
            Scribe_Values.Look(ref showStockpileZoneLabels, "showStockpileZoneLabels", true);
            Scribe_Values.Look(ref maxFontScale, "maxFontScale", 1f);
            Scribe_Values.Look(ref minFontScale, "minFontScale", 0.2f);
            Scribe_Values.Look(ref maxAllowedZoom, "maxAllowedZoom", CameraZoomRange.Furthest);
            Scribe_Values.Look(ref hideLabelsInScreenshots, "hideLabelsInScreenshots", false);
            
            // Legacy compatibility: if enabled is false, set mode to Hidden
            if (Scribe.mode == LoadSaveMode.LoadingVars && !enabled && visibilityMode == LabelVisibilityMode.Visible)
            {
                visibilityMode = LabelVisibilityMode.Hidden;
            }
            
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