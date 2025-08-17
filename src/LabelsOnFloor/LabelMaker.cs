using RimWorld;
using Verse;

namespace LabelsOnFloor
{
    public class LabelMaker
    {
        private readonly CustomRoomLabelManagerComponent _customRoomLabelManager;
        private readonly CustomZoneLabelManagerComponent _customZoneLabelManager;

        public LabelMaker(CustomRoomLabelManagerComponent customRoomLabelManager, CustomZoneLabelManagerComponent customZoneLabelManager)
        {
            _customRoomLabelManager = customRoomLabelManager;
            _customZoneLabelManager = customZoneLabelManager;
        }

        public string GetRoomLabel(Room room)
        {
            if (room == null)
                return string.Empty;

            return _customRoomLabelManager.IsRoomCustomised(room)
                ? _customRoomLabelManager.GetCustomLabelFor(room)
                : room.Role.label.ToUpper();
        }

        public string GetZoneLabel(Zone zone)
        {
            if (zone == null)
                return string.Empty;

            // Check for custom label first
            if (_customZoneLabelManager != null && _customZoneLabelManager.IsZoneCustomised(zone))
                return _customZoneLabelManager.GetCustomLabelFor(zone);

            // For growing zones, always show the plant name (unless customized above)
            if (zone is Zone_Growing growingZone)
            {
                var plantDef = growingZone.GetPlantDefToGrow();
                if (plantDef != null)
                    return plantDef.label?.ToUpper() ?? zone.label?.ToUpper() ?? string.Empty;
            }

            // Use the zone's label for all other zone types (stockpiles, etc.)
            return zone.label?.ToUpper() ?? string.Empty;
        }
    }
}