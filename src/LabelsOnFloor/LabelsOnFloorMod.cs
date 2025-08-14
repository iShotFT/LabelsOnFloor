using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

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
        private readonly FontHandler _fontHandler = new FontHandler();
        private CustomRoomLabelManagerComponent _customRoomLabelManager;
        
        // Static constructor for early initialization
        static LabelsOnFloorMod()
        {
            // Initialize Harmony patches here
            var harmony = new Harmony("LabelsOnFloor");
            harmony.PatchAll();
            ModLog.Message("LabelsOnFloor: Harmony patches applied");
            
            // Initialize the Main compatibility wrapper
            _ = new Main();
        }
        
        public LabelsOnFloorMod(ModContentPack content) : base(content)
        {
            Instance = this;
            Settings = GetSettings<LabelsOnFloorSettings>();
            ModLog.Message("LabelsOnFloor: Mod initialized");
        }
        
        public void InitializeWorldComponents(CustomRoomLabelManagerComponent customRoomLabelManager)
        {
            _customRoomLabelManager = customRoomLabelManager;
            
            LabelPlacementHandler = new LabelPlacementHandler(
                _labelHolder,
                new MeshHandler(_fontHandler),
                new LabelMaker(_customRoomLabelManager),
                new RoomRoleFinder(_customRoomLabelManager)
            );
            
            _labelDrawer = new LabelDrawer(_labelHolder, _fontHandler);
            ModLog.Message("LabelsOnFloor: World components initialized");
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
        
        public CustomRoomLabelManagerComponent GetCustomRoomLabelManager()
        {
            return _customRoomLabelManager;
        }
        
        public void Draw()
        {
            if (!IsModActive())
            {
                LabelPlacementHandler?.SetDirty();
                return;
            }
            
            // Check if we're properly initialized
            if (LabelPlacementHandler == null || _labelDrawer == null || _customRoomLabelManager == null)
            {
                // Try to initialize if we have a world
                if (Find.World != null)
                {
                    var customRoomLabelManager = Find.World.GetComponent<CustomRoomLabelManagerComponent>();
                    if (customRoomLabelManager != null && LabelPlacementHandler == null)
                    {
                        InitializeWorldComponents(customRoomLabelManager);
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
            _labelDrawer.Draw();
        }
        
        public bool IsModActive()
        {
            return Settings.enabled
                   && Current.ProgramState == ProgramState.Playing
                   && Find.CurrentMap != null
                   && WorldRendererUtility.CurrentWorldRenderMode == WorldRenderMode.None;
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
                        _fontHandler?.Reset();
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
                
                generalCategory.AddSeparator();
                
                generalCategory.AddColorPicker("defaultColor", "FALCLF.DefaultLabelColor", 
                    () => Settings.defaultLabelColor,
                    v => {
                        Settings.defaultLabelColor = v;
                        _fontHandler?.Reset();
                        LabelPlacementHandler?.SetDirty();
                    }, 
                    "FALCLF.DefaultLabelColorDesc");
                
                // Use slider-dropdown for opacity
                generalCategory.AddIntSliderDropdown("opacity", "FALCLF.TextOpacity", 
                    () => Settings.opacity,
                    v => Settings.opacity = v,
                    1, 100,
                    "FALCLF.TextOpacityDesc", 
                    (val) => val + "%");
                
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
                
                // Advanced Settings Category
                var advancedCategory = settingsFramework.AddCategory("FALCLF.CategoryAdvanced", "FALCLF.CategoryAdvancedDesc");
                
                // Use slider-dropdown for font scales
                advancedCategory.AddSliderDropdown("maxFontScale", "FALCLF.MaxFontScale", 
                    () => Settings.maxFontScale,
                    v => Settings.maxFontScale = v,
                    0.1f, 5.0f, 
                    "FALCLF.MaxFontScaleDesc", 
                    (val) => val.ToString("F1"));
                    
                advancedCategory.AddSliderDropdown("minFontScale", "FALCLF.MinFontScale", 
                    () => Settings.minFontScale,
                    v => Settings.minFontScale = v,
                    0.1f, 1.0f, 
                    "FALCLF.MinFontScaleDesc", 
                    (val) => val.ToString("F1"));
                    
                advancedCategory.AddDropdown("maxZoom", "FALCLF.MaxAllowedZoom", 
                    () => Settings.maxAllowedZoom,
                    v => Settings.maxAllowedZoom = v,
                    Enum.GetValues(typeof(CameraZoomRange)).Cast<CameraZoomRange>(),
                    (zoom) => ("FALCLF.enumSetting_" + zoom.ToString()).Translate(),
                    "FALCLF.MaxAllowedZoomDesc");
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
            _fontHandler?.Reset();
            LabelPlacementHandler?.SetDirty();
        }
    }
}