using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using Verse;

namespace LabelsOnFloor
{
    // This is the actual WorldComponent used for new saves and normal operation
    // Named with "Component" suffix to avoid conflict with the legacy compatibility class
    public class CustomRoomLabelManagerComponent : WorldComponent
    {
        private List<CustomRoomData> _roomLabels = new List<CustomRoomData>();

        public CustomRoomLabelManagerComponent(World world) : base(world)
        {
        }

        public bool IsRoomCustomised(Room room)
        {
            return _roomLabels.Any(rl => rl.RoomObject == room);
        }

        public string GetCustomLabelFor(Room room)
        {
            var result = _roomLabels.FirstOrDefault(rl => rl.RoomObject == room)?.Label?.ToUpper() ?? string.Empty;

            return result;
        }
        
        public CustomRoomData GetCustomDataFor(Room room)
        {
            return _roomLabels.FirstOrDefault(rl => rl.RoomObject == room);
        }

        public CustomRoomData GetOrCreateCustomRoomDataFor(Room room, IntVec3 loc)
        {
            var result = _roomLabels.FirstOrDefault(rl => rl.RoomObject == room);
            if (result != null)
                return result;

            result = new CustomRoomData(room, Find.CurrentMap, "", loc);
            _roomLabels.Add(result);
            result = _roomLabels.FirstOrDefault(rl => rl.RoomObject == room);

            return result;
        }

        public void CleanupMissingRooms()
        {
            _roomLabels.ForEach(d => d.AllocateRoomObjectIfNeeded());
            _roomLabels.RemoveAll(data => !data.IsRoomStillValid() || string.IsNullOrEmpty(data.Label));
        }
        
        // Called by LegacyCustomRoomLabelManager to migrate old save data
        public void MigrateFromLegacy(List<CustomRoomData> legacyRoomLabels)
        {
            if (legacyRoomLabels != null && legacyRoomLabels.Count > 0 && (_roomLabels == null || _roomLabels.Count == 0))
            {
                _roomLabels = new List<CustomRoomData>(legacyRoomLabels);
                Log.Message($"[LabelsOnFloor] Migrated {_roomLabels.Count} custom room labels from legacy save format");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref _roomLabels, "roomLabels", LookMode.Deep);
        }
    }
}