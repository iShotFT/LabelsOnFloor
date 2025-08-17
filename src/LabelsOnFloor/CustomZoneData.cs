using System.Linq;
using UnityEngine;
using Verse;

namespace LabelsOnFloor
{
    public class CustomZoneData : IExposable
    {
        public string Label;
        public Color? CustomColor;
        public int ZoneId;
        private Map _map;

        public CustomZoneData(Zone zone, Map map, string label)
        {
            ZoneId = zone.ID;
            _map = map;
            Label = label;
        }

        // Needed by save/load logic
        public CustomZoneData()
        {
        }

        public Zone GetZone()
        {
            if (_map == null)
                return null;
            
            return _map.zoneManager.AllZones.FirstOrDefault(z => z.ID == ZoneId);
        }

        public bool IsZoneStillValid()
        {
            return GetZone() != null;
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
            Scribe_Values.Look(ref ZoneId, "zoneId");
        }
    }
}