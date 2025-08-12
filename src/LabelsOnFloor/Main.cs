using HugsLib.Settings;
using HugsLib.Utils;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace LabelsOnFloor
{
    public class Main : HugsLib.ModBase
    {

        public LabelPlacementHandler LabelPlacementHandler;

        internal new ModLogger Logger => base.Logger;

        internal static Main Instance { get; private set; }

        public override string ModIdentifier => "LabelsOnFloor";

        private SettingHandle<bool> _enabled;

        private SettingHandle<Color> _defaultLabelColor;

        private SettingHandle<int> _opacity;

        private SettingHandle<bool> _showRoomLabels;

        private SettingHandle<bool> _showZoneLabels;

        private SettingHandle<bool> _showGrowingZoneLabels;

        private SettingHandle<bool> _showStockpileZoneLabels;

        private SettingHandle<float> _maxFontScale;

        private SettingHandle<float> _minFontScale;

        private SettingHandle<CameraZoomRange> _maxAllowedZoom;

        private readonly LabelHolder _labelHolder = new LabelHolder();

        private LabelDrawer _labelDrawer;

        private readonly FontHandler _fontHandler = new FontHandler();

        private CustomRoomLabelManagerComponent _customRoomLabelManager;


        public Main()
        {
            Instance = this;
        }

        public Dialog_RenameRoomWithColor GetRoomRenamer(Room room, IntVec3 loc)
        {
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
            if (!IsModAcitve())
            {
                LabelPlacementHandler.SetDirty();
                return;
            }

            if (Find.CameraDriver.CurrentZoom > _maxAllowedZoom)
                return;

            LabelPlacementHandler.RegenerateIfNeeded(_customRoomLabelManager);
            _labelDrawer.Draw();
        }

        public bool IsModAcitve()
        {
            return _enabled
                   && Current.ProgramState == ProgramState.Playing
                   && Find.CurrentMap != null
                   && WorldRendererUtility.CurrentWorldRenderMode == WorldRenderMode.None;
        }

        public Color GetDefaultLabelColor()
        {
            return _defaultLabelColor ?? Color.white;
        }

        public float GetOpacity()
        {
            return _opacity / 100f;
        }

        public bool ShowRoomNames()
        {
            return _showRoomLabels;
        }

        public bool ShowZoneNames()
        {
            return _showZoneLabels;
        }

        public bool ShowGrowingZoneLabels()
        {
            return _showZoneLabels && _showGrowingZoneLabels;
        }

        public bool ShowStockpileZoneLabels()
        {
            return _showZoneLabels && _showStockpileZoneLabels;
        }

        public float GetMaxFontScale()
        {
            return _maxFontScale;
        }

        public float GetMinFontScale()
        {
            return _minFontScale;
        }
        
        public SettingHandle<bool> GetEnabledSetting()
        {
            return _enabled;
        }

        public override void OnGUI()
        {
            if (WorldRendererUtility.CurrentWorldRenderMode != WorldRenderMode.None)
                LabelPlacementHandler?.SetDirty();

            base.OnGUI();
        }

        public override void WorldLoaded()
        {
            base.WorldLoaded();

            _customRoomLabelManager = Find.World.GetComponent<CustomRoomLabelManagerComponent>();

            LabelPlacementHandler = new LabelPlacementHandler(
                _labelHolder,
                new MeshHandler(_fontHandler),
                new LabelMaker(_customRoomLabelManager),
                new RoomRoleFinder(_customRoomLabelManager)
            );

            _labelDrawer = new LabelDrawer(_labelHolder, _fontHandler);

        }

        public override void MapLoaded(Map map)
        {
            base.MapLoaded(map);
            LabelPlacementHandler.SetDirty();
        }

        public override void DefsLoaded()
        {
            _enabled = Settings.GetHandle(
                "enabled", "FALCLF.Enabled".Translate(),
                "FALCLF.EnabledDesc".Translate(), true);

            _defaultLabelColor = Settings.GetHandle(
                "defaultLabelColor", "FALCLF.DefaultLabelColor".Translate(),
                "FALCLF.DefaultLabelColorDesc".Translate(), Color.white);
            
            _defaultLabelColor.CustomDrawer = rect => ColorSettingDrawer(rect, _defaultLabelColor);
            _defaultLabelColor.ValueChanged += val => { _fontHandler?.Reset(); };

            _opacity = Settings.GetHandle(
                "opacity", "FALCLF.TextOpacity".Translate(),
                "FALCLF.TextOpacityDesc".Translate(), 30,
                Validators.IntRangeValidator(1, 100));

            _opacity.ValueChanged += val => { _fontHandler?.Reset(); };


            _showRoomLabels = Settings.GetHandle(
                "showRoomLabels", "FALCLF.ShowRoomLabels".Translate(),
                "FALCLF.ShowRoomLabelsDesc".Translate(), true);

            _showZoneLabels = Settings.GetHandle(
                "showZoneLabels", "FALCLF.ShowZoneLabels".Translate(),
                "FALCLF.ShowZoneLabelsDesc".Translate(), true);

            _showGrowingZoneLabels = Settings.GetHandle(
                "showGrowingZoneLabels", "FALCLF.ShowGrowingZoneLabels".Translate(),
                "FALCLF.ShowGrowingZoneLabelsDesc".Translate(), true);

            _showStockpileZoneLabels = Settings.GetHandle(
                "showStockpileZoneLabels", "FALCLF.ShowStockpileZoneLabels".Translate(),
                "FALCLF.ShowStockpileZoneLabelsDesc".Translate(), true);

            _maxFontScale = Settings.GetHandle(
                "maxFontScale", "FALCLF.MaxFontScale".Translate(),
                "FALCLF.MaxFontScaleDesc".Translate(), 1f,
                Validators.FloatRangeValidator(0.1f, 5.0f));

            _minFontScale = Settings.GetHandle(
                "minFontScale", "FALCLF.MinFontScale".Translate(),
                "FALCLF.MinFontScaleDesc".Translate(), 0.2f,
                Validators.FloatRangeValidator(0.1f, 1.0f));

            _maxAllowedZoom = Settings.GetHandle(
                "maxAllowedZoom", "FALCLF.MaxAllowedZoom".Translate(),
                "FALCLF.MaxAllowedZoomDesc".Translate(), CameraZoomRange.Furthest,
                null, "FALCLF.enumSetting_");


            _enabled.ValueChanged += val => { LabelPlacementHandler?.SetDirty(); };

            _showRoomLabels.ValueChanged += val => { LabelPlacementHandler?.SetDirty(); };

            _showZoneLabels.ValueChanged += val => { LabelPlacementHandler?.SetDirty(); };

            _showGrowingZoneLabels.ValueChanged += val => { LabelPlacementHandler?.SetDirty(); };

            _showStockpileZoneLabels.ValueChanged += val => { LabelPlacementHandler?.SetDirty(); };

            _maxFontScale.ValueChanged += val => { LabelPlacementHandler?.SetDirty(); };

            _minFontScale.ValueChanged += val => { LabelPlacementHandler?.SetDirty(); };
        }
        
        private bool ColorSettingDrawer(Rect rect, SettingHandle<Color> setting)
        {
            // Draw the label on the left
            Rect labelRect = new Rect(rect.x, rect.y, rect.width * 0.6f, rect.height);
            Widgets.Label(labelRect, setting.Title);
            
            // Draw the minimal color dropdown button on the right
            Rect dropdownRect = new Rect(rect.x + rect.width * 0.65f, rect.y, rect.width * 0.3f, rect.height);
            Color? currentColor = setting.Value;
            
            // Create tooltip with RGB values
            string rgbTooltip = $"RGB ({(int)(setting.Value.r * 255)}, {(int)(setting.Value.g * 255)}, {(int)(setting.Value.b * 255)})";
            
            ColorDropdownWidget.DrawMinimalColorDropdownButton(
                dropdownRect,
                ref currentColor,
                Color.white,  // Default color (white)
                (color) => {
                    if (color.HasValue)
                    {
                        setting.Value = color.Value;
                    }
                    else
                    {
                        setting.Value = Color.white; // Reset to default
                    }
                    LabelPlacementHandler?.SetDirty();
                },
                rgbTooltip
            );
            
            // Update setting if color changed
            if (currentColor.HasValue && currentColor.Value != setting.Value)
            {
                setting.Value = currentColor.Value;
            }
            
            return false; // Don't draw the default widget
        }
    }
}
