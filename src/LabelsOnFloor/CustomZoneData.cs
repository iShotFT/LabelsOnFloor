using System.Linq;
using UnityEngine;
using Verse;

namespace LabelsOnFloor
{
    public class CustomZoneData : IExposable
    {
        public string Label;
        public Color? CustomColor;
        
        // Per-zone visibility override: null = use global, true = always show, false = always hide
        public bool? ShowLabel;
        
        // Position offset from auto-calculated position: null = no offset, X/Z offset in cells
        public Vector2? PositionOffset;
        
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
            // Save/load visibility override - defaults to null for backwards compatibility
            Scribe_Values.Look(ref ShowLabel, "showLabel", null);
            
            // Save/load position offset - use temporary variable for nullable Vector2
            Vector2 tempOffset = PositionOffset ?? Vector2.zero;
            Scribe_Values.Look(ref tempOffset, "positionOffset", Vector2.zero);
            if (Scribe.mode == LoadSaveMode.LoadingVars && tempOffset != Vector2.zero)
            {
                PositionOffset = tempOffset;
            }
            
            Scribe_References.Look(ref _map, "map");
            Scribe_Values.Look(ref ZoneId, "zoneId");
        }
    }
}