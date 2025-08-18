using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using LabelsOnFloor.FontLibrary;
using LabelsOnFloor.Services;

namespace LabelsOnFloor
{
    [StaticConstructorOnStartup]
    public class LabelsOnFloorMod : Mod
    {
        public static LabelsOnFloorMod Instance { get; private set; }
        public static LabelsOnFloorSettings Settings { get; private set; }
        
        public LabelPlacementHandler LabelPlacementHandler { get; private set; }
        
        private readonly LabelHolder _labelHolder = new LabelHolder();
        private LabelDrawer _labelDrawer;
        private MeshHandlerNew _meshHandler;
        private CustomRoomLabelManagerComponent _customRoomLabelManager;
        private CustomZoneLabelManagerComponent _customZoneLabelManager;
        
        // Static constructor for early initialization
        static LabelsOnFloorMod()
        {
            // Initialize Harmony patches here
            var harmony = new Harmony("LabelsOnFloor");
            harmony.PatchAll();
            
            // Don't initialize fonts here - will be done lazily on main thread
            
            // Initialize the Main compatibility wrapper
            _ = new Main();
        }
        
        public LabelsOnFloorMod(ModContentPack content) : base(content)
        {
            Instance = this;
            Settings = GetSettings<LabelsOnFloorSettings>();
            
            // Initialize screenshot detection for mod compatibility
            ScreenshotDetector.Initialize();
        }
        
        public void InitializeWorldComponents(CustomRoomLabelManagerComponent customRoomLabelManager, CustomZoneLabelManagerComponent customZoneLabelManager)
        {
            _customRoomLabelManager = customRoomLabelManager;
            _customZoneLabelManager = customZoneLabelManager;
            
            // Initialize the new mesh handler with the selected font from settings
            _meshHandler = new MeshHandlerNew(Settings.selectedFont);
            
            LabelPlacementHandler = new LabelPlacementHandler(
                _labelHolder,
                new MeshHandlerAdapter(_meshHandler),
                new LabelMaker(_customRoomLabelManager, _customZoneLabelManager),
                new RoomRoleFinder(_customRoomLabelManager)
            );
            
            _labelDrawer = new LabelDrawer(_labelHolder, _meshHandler);
        }
        
        public Dialog_RenameRoomWithColor GetRoomRenamer(Room room, IntVec3 loc)
        {
            if (_customRoomLabelManager == null)
            {
                ModLog.Error("CustomRoomLabelManager is null when trying to get room renamer");
                return null;
            }
            return new Dialog_RenameRoomWithColor(
                _customRoomLabelManager.GetOrCreateCustomRoomDataFor(room, loc)
            );
        }
        
        public Dialog_RenameZoneWithColor GetZoneRenamer(Zone zone)
        {
            if (_customZoneLabelManager == null)
            {
                ModLog.Error("CustomZoneLabelManager is null when trying to get zone renamer");
                return null;
            }
            return new Dialog_RenameZoneWithColor(
                zone,
                _customZoneLabelManager.GetOrCreateCustomData(zone)
            );
        }
        
        public CustomRoomLabelManagerComponent GetCustomRoomLabelManager()
        {
            return _customRoomLabelManager;
        }
        
        public CustomZoneLabelManagerComponent GetCustomZoneLabelManager()
        {
            return _customZoneLabelManager;
        }
        
        public void Draw()
        {
            if (!IsModActive())
            {
                LabelPlacementHandler?.SetDirty();
                return;
            }
            
            // Screenshot compatibility: hide labels if screenshot in progress and setting enabled
            if (Settings.hideLabelsInScreenshots && ScreenshotDetector.IsScreenshotInProgress())
            {
                return;
            }
            
            // Check if we're properly initialized
            if (LabelPlacementHandler == null || _labelDrawer == null || _customRoomLabelManager == null || _customZoneLabelManager == null)
            {
                // Try to initialize if we have a world
                if (Find.World != null)
                {
                    var customRoomLabelManager = Find.World.GetComponent<CustomRoomLabelManagerComponent>();
                    var customZoneLabelManager = Find.World.GetComponent<CustomZoneLabelManagerComponent>();
                    if (customRoomLabelManager != null && customZoneLabelManager != null && LabelPlacementHandler == null)
                    {
                        InitializeWorldComponents(customRoomLabelManager, customZoneLabelManager);
                    }
                }
                // Still not initialized, skip drawing
                if (LabelPlacementHandler == null || _labelDrawer == null)
                {
                    return;
                }
            }
            
            if (Find.CameraDriver.CurrentZoom > Settings.maxAllowedZoom)
                return;
            
            LabelPlacementHandler.RegenerateIfNeeded(_customRoomLabelManager);
            
            // Draw at the appropriate altitude based on visibility mode
            bool drawOnTop = Settings.visibilityMode == LabelVisibilityMode.DrawOnTop;
            _labelDrawer.Draw(drawOnTop);
        }
        
        public bool IsModActive()
        {
            // Check if we're in any active drawing mode (Visible or DrawOnTop)
            return Settings.visibilityMode != LabelVisibilityMode.Hidden
                   && Current.ProgramState == ProgramState.Playing
                   && Find.CurrentMap != null
#if RIMWORLD_1_5
                   && !WorldRendererUtility.WorldRenderedNow;
#else
                   && WorldRendererUtility.CurrentWorldRenderMode == WorldRenderMode.None;
#endif
        }
        
        public Color GetDefaultLabelColor()
        {
            return Settings.defaultLabelColor;
        }
        
        public float GetOpacity()
        {
            return Settings.opacity / 100f;
        }
        
        public bool ShowRoomNames()
        {
            return Settings.showRoomLabels;
        }
        
        public bool ShowZoneNames()
        {
            return Settings.showZoneLabels;
        }
        
        public bool ShowGrowingZoneLabels()
        {
            return Settings.showZoneLabels && Settings.showGrowingZoneLabels;
        }
        
        public bool ShowStockpileZoneLabels()
        {
            return Settings.showZoneLabels && Settings.showStockpileZoneLabels;
        }
        
        public float GetMaxFontScale()
        {
            return Settings.maxFontScale;
        }
        
        public float GetMinFontScale()
        {
            return Settings.minFontScale;
        }
        
        private SettingsLibrary.Core.ModSettingsFramework settingsFramework;
        
        private SettingsLibrary.Core.ModSettingsFramework GetSettingsFramework()
        {
            if (settingsFramework == null)
            {
                // Create configuration
                var config = new SettingsLibrary.Core.ModSettingsConfiguration
                {
                    ShowResetButton = true,
                    ResetToDefaults = () => {
                        Settings.ResetToDefaults();
                        _meshHandler?.ClearCache();
                        LabelPlacementHandler?.SetDirty();
                        // Force recreate settings framework to refresh UI
                        settingsFramework = null;
                    }
                };
                
                settingsFramework = new SettingsLibrary.Core.ModSettingsFramework("FALCLF.ModTitle".Translate(), config);
                
                // General Settings Category
                var generalCategory = settingsFramework.AddCategory("FALCLF.CategoryGeneral", "FALCLF.CategoryGeneralDesc");
                
                generalCategory.AddCheckbox("enabled", "FALCLF.Enabled", 
                    () => Settings.enabled, 
                    v => Settings.enabled = v, 
                    "FALCLF.EnabledDesc");
                
                // Font Rendering Options Category
                var fontCategory = settingsFramework.AddCategory("FALCLF.CategoryFontRendering", "FALCLF.CategoryFontRenderingDesc");
                
                // Font selection dropdown
                fontCategory.AddControl(new SettingsLibrary.Controls.FontDropdownControl(
                    "selectedFont",
                    "FALCLF.Font",  // Don't translate here, control will translate it
                    () => Settings.selectedFont,
                    v => {
                        Settings.selectedFont = v;
                        // Update font in mesh handler
                        _meshHandler?.SetFont(v);
                        LabelPlacementHandler?.SetDirty();
                        // Font changed
                    },
                    "FALCLF.FontDesc".Translate()
                ));
                
                fontCategory.AddColorPicker("defaultColor", "FALCLF.DefaultLabelColor", 
                    () => Settings.defaultLabelColor,
                    v => {
                        Settings.defaultLabelColor = v;
                        _meshHandler?.ClearCache();
                        LabelPlacementHandler?.SetDirty();
                    }, 
                    "FALCLF.DefaultLabelColorDesc");
                
                // Use slider-dropdown for opacity
                fontCategory.AddIntSliderDropdown("opacity", "FALCLF.TextOpacity", 
                    () => Settings.opacity,
                    v => Settings.opacity = v,
                    1, 100,
                    "FALCLF.TextOpacityDesc", 
                    (val) => val + "%");
                
                // Use slider-dropdown for font scales
                fontCategory.AddSliderDropdown("maxFontScale", "FALCLF.MaxFontScale", 
                    () => Settings.maxFontScale,
                    v => {
                        Settings.maxFontScale = v;
                        LabelPlacementHandler?.SetDirty();
                    },
                    0.1f, 5.0f, 
                    "FALCLF.MaxFontScaleDesc", 
                    (val) => val.ToString("F1"));
                    
                fontCategory.AddSliderDropdown("minFontScale", "FALCLF.MinFontScale", 
                    () => Settings.minFontScale,
                    v => {
                        Settings.minFontScale = v;
                        LabelPlacementHandler?.SetDirty();
                    },
                    0.1f, 1.0f, 
                    "FALCLF.MinFontScaleDesc", 
                    (val) => val.ToString("F1"));
                    
                fontCategory.AddDropdown("maxZoom", "FALCLF.MaxAllowedZoom", 
                    () => Settings.maxAllowedZoom,
                    v => Settings.maxAllowedZoom = v,
                    Enum.GetValues(typeof(CameraZoomRange)).Cast<CameraZoomRange>(),
                    (zoom) => ("FALCLF.enumSetting_" + zoom.ToString()).Translate(),
                    "FALCLF.MaxAllowedZoomDesc");
                
                // Display Settings Category
                var displayCategory = settingsFramework.AddCategory("FALCLF.CategoryDisplay", "FALCLF.CategoryDisplayDesc");
                
                displayCategory.AddCheckbox("showRoomLabels", "FALCLF.ShowRoomLabels", 
                    () => Settings.showRoomLabels,
                    v => Settings.showRoomLabels = v,
                    "FALCLF.ShowRoomLabelsDesc");
                
                displayCategory.AddCheckbox("showZoneLabels", "FALCLF.ShowZoneLabels", 
                    () => Settings.showZoneLabels,
                    v => {
                        Settings.showZoneLabels = v;
                        // When disabling zones, also disable children
                        if (!v)
                        {
                            Settings.showGrowingZoneLabels = false;
                            Settings.showStockpileZoneLabels = false;
                        }
                    },
                    "FALCLF.ShowZoneLabelsDesc");
                
                // Sub-options for zone labels (dependent on showZoneLabels)
                // These will be disabled when parent is off
                displayCategory.AddCheckbox("showGrowingZones", "FALCLF.ShowGrowingZoneLabels", 
                    () => Settings.showGrowingZoneLabels,
                    v => Settings.showGrowingZoneLabels = v,
                    "FALCLF.ShowGrowingZoneLabelsDesc")
                    .DependsOn(() => Settings.showZoneLabels)
                    .SetIndentLevel(1);
                    
                displayCategory.AddCheckbox("showStockpileZones", "FALCLF.ShowStockpileZoneLabels", 
                    () => Settings.showStockpileZoneLabels,
                    v => Settings.showStockpileZoneLabels = v,
                    "FALCLF.ShowStockpileZoneLabelsDesc")
                    .DependsOn(() => Settings.showZoneLabels)
                    .SetIndentLevel(1);
                    
                // Mod Compatibility Category (only shown if compatible mods detected)
                if (ScreenshotDetector.ShouldShowCompatibilityCategory())
                {
                    var compatCategory = settingsFramework.AddCategory("FALCLF.CategoryCompatibility", "FALCLF.CategoryCompatibilityDesc");
                    
                    compatCategory.AddCheckbox("hideLabelsInScreenshots", "FALCLF.HideLabelsInScreenshots", 
                        () => Settings.hideLabelsInScreenshots,
                        v => Settings.hideLabelsInScreenshots = v,
                        "FALCLF.HideLabelsInScreenshotsDesc");
                }
            }
            
            return settingsFramework;
        }
        
        public override void DoSettingsWindowContents(Rect inRect)
        {
            GetSettingsFramework().DoSettingsWindowContents(inRect);
            
            // Validate settings after any changes
            Settings.ValidateSettings();
        }
        
        public override string SettingsCategory()
        {
            return "FALCLF.ModTitle".Translate();
        }
        
        public override void WriteSettings()
        {
            base.WriteSettings();
            // Trigger any necessary updates when settings are saved
            _meshHandler?.ClearCache();
            LabelPlacementHandler?.SetDirty();
        }
    }
}