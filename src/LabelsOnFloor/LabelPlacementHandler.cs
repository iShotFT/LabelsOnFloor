using System;
using System.Linq;
using RimWorld;
using UnityEngine.Assertions.Must;
using Verse;

namespace LabelsOnFloor
{
    public class LabelPlacementHandler
    {
        private readonly LabelHolder _labelHolder;

        private readonly LabelMaker _labelMaker;

        private readonly RoomRoleFinder _roomRoleFinder;

        private readonly MeshHandler _meshHandler;

        private Map _map;

        private bool _ready;

        public LabelPlacementHandler(LabelHolder labelHolder, MeshHandler meshHandler, 
            LabelMaker labelMaker, RoomRoleFinder roomRoleFinder)
        {
            _labelHolder = labelHolder;
            _meshHandler = meshHandler;
            _labelMaker = labelMaker;
            _roomRoleFinder = roomRoleFinder;
        }

        public void SetDirty()
        {
            _labelHolder.Clear();
            _ready = false;
        }

        public void SetDirtyIfAreaIsOnMap(Map map)
        {
            if (map == null || map == _map)
            {
                SetDirty();
            }
        }

        public void RegenerateIfNeeded(CustomRoomLabelManagerComponent customRoomLabelManager)
        {
            if (_ready && _map == Find.CurrentMap)
                return;

            customRoomLabelManager.CleanupMissingRooms();

            _map = Find.CurrentMap;
            _labelHolder.Clear();
            _ready = true;

            RegenerateRoomLabels();
            RegenerateZoneLabels();
        }

        public void AddOrUpdateRoom(Room room)
        {
            AddOrUpdateRoom(room, null);
        }

        public void AddOrUpdateRoom(Room room, PlacementDataFinderForRooms placementDataFinderForRooms)
        {
            if (!_ready || room == null)
                return;

            if (room.Map != _map)
                return;

            if (room.Fogged || !_roomRoleFinder.IsImportantRoom(room))
                return;
            
            // Check per-room visibility override
            var customRoomLabelManager = Main.Instance.GetCustomRoomLabelManager();
            if (customRoomLabelManager != null)
            {
                var customData = customRoomLabelManager.GetCustomDataFor(room);
                if (customData != null && customData.ShowLabel.HasValue)
                {
                    // Room has explicit visibility override
                    if (!customData.ShowLabel.Value)
                        return; // Room is explicitly hidden
                    // If ShowLabel is true, continue to show the label regardless of global setting
                }
                else
                {
                    // No override, use global setting
                    if (!Main.Instance.ShowRoomNames())
                        return;
                }
            }
            else
            {
                // No custom manager, use global setting
                if (!Main.Instance.ShowRoomNames())
                    return;
            }

            var text = _labelMaker.GetRoomLabel(room);
            if (placementDataFinderForRooms == null)
            {
                placementDataFinderForRooms = new PlacementDataFinderForRooms(_map);
            }

            var label = AddLabelForArea(room, text, () => 
            {
                var baseData = placementDataFinderForRooms.GetData(room, text.Length);
                if (baseData == null)
                    return null;
                    
                // Apply position offset if available
                if (customRoomLabelManager != null)
                {
                    var customData = customRoomLabelManager.GetCustomDataFor(room);
                    if (customData?.PositionOffset != null)
                    {
                        // Create a new PlacementData with the offset applied
                        var offsetData = new PlacementData
                        {
                            Position = new IntVec3(
                                baseData.Position.x + (int)customData.PositionOffset.Value.x,
                                baseData.Position.y,
                                baseData.Position.z + (int)customData.PositionOffset.Value.y
                            ),
                            Scale = baseData.Scale,
                            Flipped = baseData.Flipped
                        };
                        return offsetData;
                    }
                }
                return baseData;
            });
            
            // Apply custom color if available
            if (label != null && customRoomLabelManager != null)
            {
                var customData = customRoomLabelManager.GetCustomDataFor(room);
                if (customData != null && customData.CustomColor.HasValue)
                {
                    label.CustomColor = customData.CustomColor;
                }
            }
        }

        public void AddOrUpdateZone(Zone zone)
        {
            if (!_ready || zone == null)
                return;

            if (zone.Map != _map)
                return;

            // Check per-zone visibility override
            var customZoneLabelManager = Main.Instance.GetCustomZoneLabelManager();
            if (customZoneLabelManager != null)
            {
                var customData = customZoneLabelManager.GetCustomDataFor(zone);
                if (customData != null && customData.ShowLabel.HasValue)
                {
                    // Zone has explicit visibility override
                    if (!customData.ShowLabel.Value)
                        return; // Zone is explicitly hidden
                    // If ShowLabel is true, continue to show the label regardless of global setting
                }
                else
                {
                    // No override, use global settings
                    // Check granular zone type settings
                    if (zone is Zone_Growing)
                    {
                        if (!Main.Instance.ShowGrowingZoneLabels())
                            return;
                    }
                    else if (zone is Zone_Stockpile)
                    {
                        if (!Main.Instance.ShowStockpileZoneLabels())
                            return;
                    }
                    // For any other zone types, use the general zone setting
                    else if (!Main.Instance.ShowZoneNames())
                    {
                        return;
                    }
                }
            }
            else
            {
                // No custom manager, use global settings
                // Check granular zone type settings
                if (zone is Zone_Growing)
                {
                    if (!Main.Instance.ShowGrowingZoneLabels())
                        return;
                }
                else if (zone is Zone_Stockpile)
                {
                    if (!Main.Instance.ShowStockpileZoneLabels())
                        return;
                }
                // For any other zone types, use the general zone setting
                else if (!Main.Instance.ShowZoneNames())
                {
                    return;
                }
            }

            var text = _labelMaker.GetZoneLabel(zone);
            var addedLabel = 
                AddLabelForArea(zone, text, () => 
                {
                    var baseData = PlacementDataFinderForZones.GetData(zone, _map, text.Length);
                    if (baseData == null)
                        return null;
                        
                    // Apply position offset if available
                    if (customZoneLabelManager != null)
                    {
                        var customData = customZoneLabelManager.GetCustomDataFor(zone);
                        if (customData?.PositionOffset != null)
                        {
                            // Create a new PlacementData with the offset applied
                            var offsetData = new PlacementData
                            {
                                Position = new IntVec3(
                                    baseData.Position.x + (int)customData.PositionOffset.Value.x,
                                    baseData.Position.y,
                                    baseData.Position.z + (int)customData.PositionOffset.Value.y
                                ),
                                Scale = baseData.Scale,
                                Flipped = baseData.Flipped
                            };
                            return offsetData;
                        }
                    }
                    return baseData;
                });

            if (addedLabel != null)
            {
                addedLabel.IsZone = true;
                
                // Apply custom color if zone has one
                if (customZoneLabelManager != null)
                {
                    var customColor = customZoneLabelManager.GetCustomColorFor(zone);
                    if (customColor.HasValue)
                    {
                        addedLabel.CustomColor = customColor;
                    }
                }
            }
        }

        private Label AddLabelForArea(object area, string text, Func<PlacementData> placementDataGetter)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            _labelHolder.RemoveLabelForArea(area);

            var label = new Label
            {
                LabelMesh = _meshHandler.GetMeshFor(text),
                LabelPlacementData = placementDataGetter(),
                AssociatedArea = area
            };

            if (!label.IsValid())
                return null;

            _labelHolder.Add(label);
            return label;
        }

        private void RegenerateRoomLabels()
        {
            if (!Main.Instance.ShowRoomNames())
                return;

            var roomPlacementDataFinder = new PlacementDataFinderForRooms(_map);
#if RIMWORLD_1_5
            foreach (var room in _map.regionGrid.allRooms)
#else
            foreach (var room in _map.regionGrid.AllRooms)
#endif
            {
                AddOrUpdateRoom(room, roomPlacementDataFinder);
            }
        }

        private void RegenerateZoneLabels()
        {
            if (!Main.Instance.ShowZoneNames())
                return;

            foreach (var zone in _map.zoneManager.AllZones)
            {
                AddOrUpdateZone(zone);
            }
        }

    }
}