using System;
using System.Collections.Generic;
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
        
        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);
            
            // Main toggle
            listing.CheckboxLabeled("FALCLF.Enabled".Translate(), ref Settings.enabled, 
                "FALCLF.EnabledDesc".Translate());
            
            listing.GapLine();
            
            // Color picker
            Rect colorRect = listing.GetRect(30f);
            Rect labelRect = colorRect.LeftPart(0.5f);
            Rect dropdownRect = colorRect.RightPart(0.45f);
            
            Widgets.Label(labelRect, "FALCLF.DefaultLabelColor".Translate());
            TooltipHandler.TipRegion(labelRect, "FALCLF.DefaultLabelColorDesc".Translate());
            
            Color? tempColor = Settings.defaultLabelColor;
            ColorDropdownWidget.DrawMinimalColorDropdownButton(
                dropdownRect, ref tempColor, Color.white,
                (color) => {
                    if (color.HasValue)
                    {
                        Settings.defaultLabelColor = color.Value;
                        _fontHandler?.Reset();
                        LabelPlacementHandler?.SetDirty();
                    }
                },
                $"RGB ({(int)(Settings.defaultLabelColor.r * 255)}, " +
                $"{(int)(Settings.defaultLabelColor.g * 255)}, " +
                $"{(int)(Settings.defaultLabelColor.b * 255)})"
            );
            if (tempColor.HasValue && tempColor != Settings.defaultLabelColor)
            {
                Settings.defaultLabelColor = tempColor.Value;
                _fontHandler?.Reset();
                LabelPlacementHandler?.SetDirty();
            }
            
            listing.Gap();
            
            // Opacity slider
            listing.Label("FALCLF.TextOpacity".Translate() + ": " + Settings.opacity + "%");
            TooltipHandler.TipRegion(listing.GetRect(0f), "FALCLF.TextOpacityDesc".Translate());
            Settings.opacity = (int)listing.Slider(Settings.opacity, 1, 100);
            
            listing.GapLine();
            
            // Checkboxes for features
            listing.CheckboxLabeled("FALCLF.ShowRoomLabels".Translate(), 
                ref Settings.showRoomLabels, "FALCLF.ShowRoomLabelsDesc".Translate());
            
            listing.CheckboxLabeled("FALCLF.ShowZoneLabels".Translate(), 
                ref Settings.showZoneLabels, "FALCLF.ShowZoneLabelsDesc".Translate());
            
            if (Settings.showZoneLabels)
            {
                listing.Indent(20f);
                listing.CheckboxLabeled("FALCLF.ShowGrowingZoneLabels".Translate(), 
                    ref Settings.showGrowingZoneLabels, "FALCLF.ShowGrowingZoneLabelsDesc".Translate());
                listing.CheckboxLabeled("FALCLF.ShowStockpileZoneLabels".Translate(), 
                    ref Settings.showStockpileZoneLabels, "FALCLF.ShowStockpileZoneLabelsDesc".Translate());
                listing.Outdent(20f);
            }
            
            listing.GapLine();
            
            // Font scale sliders
            listing.Label("FALCLF.MaxFontScale".Translate() + ": " + Settings.maxFontScale.ToString("F1"));
            TooltipHandler.TipRegion(listing.GetRect(0f), "FALCLF.MaxFontScaleDesc".Translate());
            Settings.maxFontScale = listing.Slider(Settings.maxFontScale, 0.1f, 5.0f);
            
            listing.Label("FALCLF.MinFontScale".Translate() + ": " + Settings.minFontScale.ToString("F1"));
            TooltipHandler.TipRegion(listing.GetRect(0f), "FALCLF.MinFontScaleDesc".Translate());
            Settings.minFontScale = listing.Slider(Settings.minFontScale, 0.1f, 1.0f);
            
            listing.GapLine();
            
            // Zoom range dropdown
            if (listing.ButtonTextLabeled("FALCLF.MaxAllowedZoom".Translate(), 
                ("FALCLF.enumSetting_" + Settings.maxAllowedZoom.ToString()).Translate()))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (CameraZoomRange zoom in Enum.GetValues(typeof(CameraZoomRange)))
                {
                    CameraZoomRange localZoom = zoom;
                    options.Add(new FloatMenuOption(
                        ("FALCLF.enumSetting_" + localZoom.ToString()).Translate(),
                        () => Settings.maxAllowedZoom = localZoom
                    ));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            TooltipHandler.TipRegion(listing.GetRect(0f), "FALCLF.MaxAllowedZoomDesc".Translate());
            
            listing.End();
            
            // Validate settings
            Settings.ValidateSettings();
        }
        
        public override string SettingsCategory()
        {
            return "Labels On Floor";
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