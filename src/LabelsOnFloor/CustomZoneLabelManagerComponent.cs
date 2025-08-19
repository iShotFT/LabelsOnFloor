using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld.Planet;

namespace LabelsOnFloor
{
    public class CustomZoneLabelManagerComponent : WorldComponent
    {
        private List<CustomZoneData> _zoneLabels = new List<CustomZoneData>();

        public CustomZoneLabelManagerComponent(World world) : base(world)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref _zoneLabels, "customZoneLabels", LookMode.Deep);
            if (_zoneLabels == null)
            {
                _zoneLabels = new List<CustomZoneData>();
            }
        }

        public CustomZoneData GetCustomDataFor(Zone zone)
        {
            if (zone == null)
                return null;

            // Try to find existing data - only check ID since zone references can change
            return _zoneLabels.FirstOrDefault(data => 
                data != null && data.ZoneId == zone.ID);
        }

        public CustomZoneData GetOrCreateCustomData(Zone zone)
        {
            if (zone == null)
                return null;

            // Try to find existing data - only check ID since zone references can change
            var existingData = _zoneLabels.FirstOrDefault(data => 
                data != null && data.ZoneId == zone.ID);
            
            if (existingData != null)
                return existingData;

            // Create new data with empty label (no custom label yet)
            var newData = new CustomZoneData(zone, zone.Map, "");
            _zoneLabels.Add(newData);
            return newData;
        }

        public bool IsZoneCustomised(Zone zone)
        {
            if (zone == null || _zoneLabels == null)
                return false;

            var data = _zoneLabels.FirstOrDefault(d => 
                d != null && d.ZoneId == zone.ID);
            return data != null && (!string.IsNullOrEmpty(data.Label) || data.CustomColor.HasValue || data.ShowLabel.HasValue || data.PositionOffset.HasValue);
        }

        public string GetCustomLabelFor(Zone zone)
        {
            if (zone == null || _zoneLabels == null)
                return string.Empty;

            var data = _zoneLabels.FirstOrDefault(d => 
                d != null && d.ZoneId == zone.ID);
            return data?.Label ?? zone.label?.ToUpper() ?? string.Empty;
        }

        public Color? GetCustomColorFor(Zone zone)
        {
            if (zone == null || _zoneLabels == null)
                return null;

            var data = _zoneLabels.FirstOrDefault(d => 
                d != null && d.ZoneId == zone.ID);
            return data?.CustomColor;
        }

        public void CleanupMissingZones()
        {
            if (_zoneLabels != null)
            {
                // Only remove if zone is invalid OR if there's no customization at all
                _zoneLabels.RemoveAll(data => data == null || !data.IsZoneStillValid() ||
                    (string.IsNullOrEmpty(data.Label) && !data.CustomColor.HasValue && !data.ShowLabel.HasValue && !data.PositionOffset.HasValue));
            }
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            
            // Cleanup every 5 seconds (300 ticks)
            if (Find.TickManager.TicksGame % 300 == 0)
            {
                CleanupMissingZones();
            }
        }
    }
}