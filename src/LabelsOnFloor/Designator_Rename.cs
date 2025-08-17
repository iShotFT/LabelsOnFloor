﻿using RimWorld;
using UnityEngine;
using Verse;

namespace LabelsOnFloor
{
    public class Designator_Rename : Designator
    {
        public Designator_Rename()
        {
            defaultLabel = "Rename".Translate();
            defaultDesc = "FALCLF.RenameDesc".Translate();
            icon = Resources.Rename;
            useMouseIcon = true;
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            var map = Find.CurrentMap;
            return RoomRoleFinder.GetRoomAtLocation(loc, map) != null 
                || loc.GetZone(map) != null;
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            var map = Find.CurrentMap;

            var zone = c.GetZone(map);
            if (zone != null)
            {
                Find.WindowStack.Add(Main.Instance.GetZoneRenamer(zone));
                return;
            }

            var room = c.GetRoom(map);
            if (room != null)
                Find.WindowStack.Add(Main.Instance.GetRoomRenamer(room, c));
        }
    }
}