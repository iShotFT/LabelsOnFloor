using Verse;

namespace LabelsOnFloor
{
    public class MapComponent_LabelsOnFloor : MapComponent
    {
        public MapComponent_LabelsOnFloor(Map map) : base(map)
        {
        }
        
        public override void MapComponentOnGUI()
        {
            // Draw labels for this specific map
            if (map == Find.CurrentMap)
            {
                LabelsOnFloorMod.Instance?.Draw();
            }
        }
        
        public override void FinalizeInit()
        {
            // Replaces MapLoaded() from HugsLib
            base.FinalizeInit();
            LabelsOnFloorMod.Instance?.LabelPlacementHandler?.SetDirty();
            ModLog.Message($"Map component initialized for map {map.uniqueID}");
        }
        
        public override void MapRemoved()
        {
            base.MapRemoved();
            // Clean up when map is removed
            LabelsOnFloorMod.Instance?.LabelPlacementHandler?.SetDirty();
        }
    }
}