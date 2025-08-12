﻿using UnityEngine;
using Verse;

namespace LabelsOnFloor
{
    public class CustomRoomData : IExposable
    {
        public string Label;
        
        public Color? CustomColor;

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

            return _keyCell.GetRoom(_map) == RoomObject;
        }

        public void AllocateRoomObjectIfNeeded()
        {
            if (RoomObject != null || _map == null)
                return;

            RoomObject = _keyCell.GetRoom(_map);
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
            Scribe_References.Look(ref _map, "map");
            Scribe_Values.Look(ref _keyCell, "keyCell");
        }
    }
}