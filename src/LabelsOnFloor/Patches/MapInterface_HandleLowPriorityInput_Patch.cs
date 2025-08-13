using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace LabelsOnFloor.Patches
{
    /// <summary>
    /// Patch to handle custom hotkeys for the Labels On Floor mod
    /// </summary>
    [HarmonyPatch(typeof(MapInterface), "HandleLowPriorityInput")]
    public static class MapInterface_HandleLowPriorityInput_Patch
    {
        public static void Postfix()
        {
            // Only process hotkeys when the game is playing and map is visible
            if (Find.CurrentMap == null || !WorldRendererUtility.DrawingMap)
                return;
            
            // Handle Rename Room hotkey (default: R)
            if (KeyBindingDefOf_LabelsOnFloor.LabelsOnFloor_RenameRoom.KeyDownEvent)
            {
                HandleRenameRoomHotkey();
            }
            
            // Handle Toggle Labels hotkey (default: L)
            if (KeyBindingDefOf_LabelsOnFloor.LabelsOnFloor_ToggleLabels.KeyDownEvent)
            {
                HandleToggleLabelsHotkey();
            }
        }
        
        private static void HandleRenameRoomHotkey()
        {
            // Only activate if no pawns are selected (to avoid conflict with draft command)
            if (Find.Selector.SelectedPawns.Count > 0)
                return;
            
            // Get the cell under the mouse cursor
            IntVec3 cellUnderMouse = UI.MouseCell();
            
            // Check if the cell is valid
            if (!cellUnderMouse.InBounds(Find.CurrentMap))
                return;
            
            // Try to get a zone first
            Zone zone = cellUnderMouse.GetZone(Find.CurrentMap);
            if (zone != null)
            {
                // Consume the event to prevent 'R' from appearing in the text field
                Event.current.Use();
                
                // Open the dialog on the next frame to avoid input propagation
                var zoneDialog = new Dialog_RenameZone(zone);
                // Dialog_RenameZone inherits from Dialog_Rename<Zone> which has WasOpenedByHotkey
                zoneDialog.WasOpenedByHotkey();
                Find.WindowStack.Add(zoneDialog);
                return;
            }
            
            // Try to get a room
            Room room = cellUnderMouse.GetRoom(Find.CurrentMap);
            if (room != null && room.Role != RoomRoleDefOf.None)
            {
                var roomDialog = Main.Instance.GetRoomRenamer(room, cellUnderMouse);
                if (roomDialog != null)
                {
                    // Consume the event to prevent 'R' from appearing in the text field
                    Event.current.Use();
                    
                    // Mark dialog as opened by hotkey to skip first input
                    if (roomDialog is Dialog_RenameRoomWithColor renameRoom)
                    {
                        renameRoom.WasOpenedByHotkey();
                    }
                    Find.WindowStack.Add(roomDialog);
                }
            }
        }
        
        private static void HandleToggleLabelsHotkey()
        {
            // Toggle the main enabled setting
            if (LabelsOnFloorMod.Instance != null && LabelsOnFloorMod.Settings != null)
            {
                LabelsOnFloorMod.Settings.enabled = !LabelsOnFloorMod.Settings.enabled;
                LabelsOnFloorMod.Instance.WriteSettings(); // Save the change
                
                // Show a message to indicate the state change
                string messageKey = LabelsOnFloorMod.Settings.enabled ? "FALCLF.LabelsEnabled" : "FALCLF.LabelsDisabled";
                Messages.Message(messageKey.Translate(), MessageTypeDefOf.SilentInput, false);
            }
        }
    }
}