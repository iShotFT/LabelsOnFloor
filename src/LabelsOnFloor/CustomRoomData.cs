using UnityEngine;
using Verse;

namespace LabelsOnFloor
{
    public class CustomRoomData : IExposable
    {
        public string Label;
        
        public Color? CustomColor;
        
        // Per-room visibility override: null = use global, true = always show, false = always hide
        public bool? ShowLabel;

        public Room RoomObject;

        private Map _map;

        private IntVec3 _keyCell;


        public CustomRoomData(Room roomObject, Map map, string label, IntVec3 keyCell)
        {
            RoomObject = roomObject;
            _map = map;
            Label = label;
            _keyCell = keyCell;
        }

        // Needed by save/load logic
        public CustomRoomData()
        {
            
        }

        public bool IsRoomStillValid()
        {
            if (RoomObject == null || _map == null)
                return false;

            // Check if map is properly initialized with regions
            if (_map.regionAndRoomUpdater == null || !_map.regionAndRoomUpdater.Enabled)
                return false;
            
            // Safely get the room at the key cell
            try
            {
                var currentRoom = _keyCell.GetRoom(_map);
                return currentRoom == RoomObject;
            }
            catch
            {
                // If we can't get the room, it's not valid
                return false;
            }
        }

        public void AllocateRoomObjectIfNeeded()
        {
            if (RoomObject != null || _map == null)
                return;

            // Check if map is properly initialized with regions
            if (_map.regionAndRoomUpdater == null || !_map.regionAndRoomUpdater.Enabled)
                return;

            try
            {
                RoomObject = _keyCell.GetRoom(_map);
            }
            catch
            {
                // If we can't get the room, leave RoomObject as null
                RoomObject = null;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref Label, "label", "");
            Color tempColor = CustomColor ?? Color.white;
            Scribe_Values.Look(ref tempColor, "customColor", Color.white);
            if (Scribe.mode == LoadSaveMode.LoadingVars && tempColor != Color.white)
            {
                CustomColor = tempColor;
            }
            // Save/load visibility override - defaults to null for backwards compatibility
            Scribe_Values.Look(ref ShowLabel, "showLabel", null);
            Scribe_References.Look(ref _map, "map");
            Scribe_Values.Look(ref _keyCell, "keyCell");
        }
    }
}