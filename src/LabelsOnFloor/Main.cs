using UnityEngine;
using Verse;

namespace LabelsOnFloor
{
    // This class is kept for backwards compatibility and redirects everything to the new LabelsOnFloorMod
    // Some Harmony patches may still reference Main.Instance
    public class Main
    {
        internal static Main Instance { get; private set; }
        
        public LabelPlacementHandler LabelPlacementHandler => LabelsOnFloorMod.Instance?.LabelPlacementHandler;
        
        public Main()
        {
            Instance = this;
        }
        
        public Dialog_RenameRoomWithColor GetRoomRenamer(Room room, IntVec3 loc)
        {
            return LabelsOnFloorMod.Instance?.GetRoomRenamer(room, loc);
        }
        
        public Dialog_RenameZoneWithColor GetZoneRenamer(Zone zone)
        {
            return LabelsOnFloorMod.Instance?.GetZoneRenamer(zone);
        }
        
        public CustomRoomLabelManagerComponent GetCustomRoomLabelManager()
        {
            return LabelsOnFloorMod.Instance?.GetCustomRoomLabelManager();
        }
        
        public CustomZoneLabelManagerComponent GetCustomZoneLabelManager()
        {
            return LabelsOnFloorMod.Instance?.GetCustomZoneLabelManager();
        }
        
        public void Draw()
        {
            LabelsOnFloorMod.Instance?.Draw();
        }
        
        public bool IsModAcitve()
        {
            return LabelsOnFloorMod.Instance?.IsModActive() ?? false;
        }
        
        public Color GetDefaultLabelColor()
        {
            return LabelsOnFloorMod.Instance?.GetDefaultLabelColor() ?? Color.white;
        }
        
        public float GetOpacity()
        {
            return LabelsOnFloorMod.Instance?.GetOpacity() ?? 0.3f;
        }
        
        public bool ShowRoomNames()
        {
            return LabelsOnFloorMod.Instance?.ShowRoomNames() ?? true;
        }
        
        public bool ShowZoneNames()
        {
            return LabelsOnFloorMod.Instance?.ShowZoneNames() ?? true;
        }
        
        public bool ShowGrowingZoneLabels()
        {
            return LabelsOnFloorMod.Instance?.ShowGrowingZoneLabels() ?? true;
        }
        
        public bool ShowStockpileZoneLabels()
        {
            return LabelsOnFloorMod.Instance?.ShowStockpileZoneLabels() ?? true;
        }
        
        public float GetMaxFontScale()
        {
            return LabelsOnFloorMod.Instance?.GetMaxFontScale() ?? 1f;
        }
        
        public float GetMinFontScale()
        {
            return LabelsOnFloorMod.Instance?.GetMinFontScale() ?? 0.2f;
        }
    }
}