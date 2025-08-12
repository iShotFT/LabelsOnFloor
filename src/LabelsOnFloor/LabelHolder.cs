using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace LabelsOnFloor
{
    public class PlacementData
    {
        public IntVec3 Position;
        public Vector3 Scale;
        public bool Flipped = false;
    }

    public class Label
    {
        public Mesh LabelMesh;
        public PlacementData LabelPlacementData;
        public object AssociatedArea;
        public bool IsZone = false;
        public Color? CustomColor = null;

        public bool IsValid()
        {
            return LabelPlacementData != null && LabelMesh != null && AssociatedArea != null;
        }
    }

    public class LabelHolder
    {
        private readonly List<Label> _currentLabels = new List<Label>();
        
        // Performance optimization: Cache the filtered results
        private List<Label> _filteredLabels = null;
        private bool _filterDirty = true;
        
        // Cache collections to avoid allocations
        private readonly HashSet<Room> _roomsToRemove = new HashSet<Room>();
        private readonly HashSet<Room> _labelledRooms = new HashSet<Room>();
        private readonly List<Zone> _zonesList = new List<Zone>();

        public void Clear()
        {
            _currentLabels.Clear();
            _filteredLabels = null;
            _filterDirty = true;
        }

        public void Add(Label label)
        {
            _currentLabels.Add(label);
            _filterDirty = true;
        }

        public void RemoveLabelForArea(object area)
        {
            _currentLabels.RemoveAll(l => l.AssociatedArea == area);
            _filterDirty = true;
        }

        public IEnumerable<Label> GetLabels()
        {
            // Return cached result if nothing changed
            if (!_filterDirty && _filteredLabels != null)
                return _filteredLabels;
                
            UpdateFilteredLabels();
            return _filteredLabels;
        }

        private void UpdateFilteredLabels()
        {
            _filterDirty = false;
            
            // Early exit: If we're not showing both types, no conflict resolution needed
            if (!Main.Instance.ShowRoomNames() || !Main.Instance.ShowZoneNames())
            {
                _filteredLabels = _currentLabels;
                return;
            }
            
            // Early exit: If no zones exist, no conflict resolution needed
            bool hasZones = false;
            for (int i = 0; i < _currentLabels.Count; i++)
            {
                if (_currentLabels[i].IsZone)
                {
                    hasZones = true;
                    break;
                }
            }
            
            if (!hasZones)
            {
                _filteredLabels = _currentLabels;
                return;
            }
            
            // Perform conflict resolution with cached collections
            RemoveRoomsWithZonesOptimized();
        }

        private void RemoveRoomsWithZonesOptimized()
        {
            var map = Find.CurrentMap;
            if (map == null)
            {
                _filteredLabels = _currentLabels;
                return;
            }
            
            // Clear reusable collections instead of allocating new ones
            _roomsToRemove.Clear();
            _labelledRooms.Clear();
            _zonesList.Clear();
            
            // First pass: Identify all labelled rooms and zones
            for (int i = 0; i < _currentLabels.Count; i++)
            {
                var label = _currentLabels[i];
                if (!label.IsZone)
                {
                    var room = label.AssociatedArea as Room;
                    if (room != null)
                        _labelledRooms.Add(room);
                }
                else
                {
                    var zone = label.AssociatedArea as Zone;
                    if (zone != null && zone.Cells.Count > 0)
                        _zonesList.Add(zone);
                }
            }
            
            // Early exit if no rooms to potentially remove
            if (_labelledRooms.Count == 0)
            {
                _filteredLabels = _currentLabels;
                return;
            }
            
            // Second pass: Find rooms that contain zones
            for (int i = 0; i < _zonesList.Count; i++)
            {
                var zone = _zonesList[i];
                // Use the first cell as representative (original logic)
                var roomWithCell = RoomRoleFinder.GetRoomAtLocation(zone.Cells.First(), map);
                
                if (roomWithCell != null && _labelledRooms.Contains(roomWithCell))
                {
                    _roomsToRemove.Add(roomWithCell);
                }
            }
            
            // Create filtered list if rooms need to be removed
            if (_roomsToRemove.Count > 0)
            {
                if (_filteredLabels == null)
                    _filteredLabels = new List<Label>(_currentLabels.Count);
                else
                    _filteredLabels.Clear();
                    
                for (int i = 0; i < _currentLabels.Count; i++)
                {
                    var label = _currentLabels[i];
                    if (!label.IsZone || !_roomsToRemove.Contains(label.AssociatedArea as Room))
                    {
                        _filteredLabels.Add(label);
                    }
                }
            }
            else
            {
                _filteredLabels = _currentLabels;
            }
        }
    }
}