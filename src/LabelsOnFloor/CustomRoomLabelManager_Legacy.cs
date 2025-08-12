using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace LabelsOnFloor
{
    // This class exists ONLY for backwards compatibility with old saves
    // Old saves have "LabelsOnFloor.CustomRoomLabelManager" stored as a WorldObject
    // This class with the EXACT SAME NAME allows those saves to load without errors
    // The actual functionality is in CustomRoomLabelManager (which is a WorldComponent)
    
    // DO NOT RENAME THIS CLASS - it must match the old save data exactly
    public class CustomRoomLabelManager : WorldObject
    {
        private List<CustomRoomData> roomLabels;
        
        // Parameterless constructor required for loading old saves
        public CustomRoomLabelManager()
        {
        }
        
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref roomLabels, "roomLabels", LookMode.Deep);
            
            // After loading, migrate data to the WorldComponent
            if (Scribe.mode == LoadSaveMode.PostLoadInit && roomLabels != null && roomLabels.Count > 0)
            {
                MigrationHelper.ScheduleMigration(this, roomLabels);
            }
        }
        
        public override void PostAdd()
        {
            base.PostAdd();
            // Schedule removal after being added
            MigrationHelper.ScheduleRemoval(this);
        }
    }
    
    internal static class MigrationHelper
    {
        private static List<(WorldObject obj, List<CustomRoomData> data)> pendingMigrations = new List<(WorldObject, List<CustomRoomData>)>();
        private static List<WorldObject> pendingRemovals = new List<WorldObject>();
        
        public static void ScheduleMigration(WorldObject obj, List<CustomRoomData> data)
        {
            pendingMigrations.Add((obj, new List<CustomRoomData>(data)));
            Log.Message($"[LabelsOnFloor] Scheduled migration of {data.Count} custom room labels from legacy save");
        }
        
        public static void ScheduleRemoval(WorldObject obj)
        {
            if (!pendingRemovals.Contains(obj))
                pendingRemovals.Add(obj);
        }
        
        public static void ProcessMigrations()
        {
            if (pendingMigrations.Count > 0)
            {
                var worldComponent = Find.World?.GetComponent<CustomRoomLabelManagerComponent>();
                if (worldComponent != null)
                {
                    foreach (var (obj, data) in pendingMigrations)
                    {
                        worldComponent.MigrateFromLegacy(data);
                    }
                    Log.Message($"[LabelsOnFloor] Successfully migrated {pendingMigrations.Count} legacy room label manager(s)");
                }
                pendingMigrations.Clear();
            }
            
            // Remove legacy objects
            foreach (var obj in pendingRemovals)
            {
                if (obj != null && !obj.Destroyed)
                {
                    obj.Destroy();
                }
            }
            pendingRemovals.Clear();
        }
    }
}