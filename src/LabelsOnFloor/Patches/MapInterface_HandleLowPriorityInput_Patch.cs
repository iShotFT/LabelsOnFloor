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
#if RIMWORLD_1_5
            if (Find.CurrentMap == null || WorldRendererUtility.WorldRenderedNow)
                return;
#else
            if (Find.CurrentMap == null || !WorldRendererUtility.DrawingMap)
                return;
#endif
            
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
                
                // Use our custom zone rename dialog with color picker
                var zoneDialog = new Dialog_RenameZoneWithColor(zone);
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
            // Cycle through the three visibility states
            if (LabelsOnFloorMod.Instance != null && LabelsOnFloorMod.Settings != null)
            {
                var settings = LabelsOnFloorMod.Settings;
                
                // Cycle to next state: Visible -> DrawOnTop -> Hidden -> Visible
                switch (settings.visibilityMode)
                {
                    case LabelVisibilityMode.Visible:
                        settings.visibilityMode = LabelVisibilityMode.DrawOnTop;
                        break;
                    case LabelVisibilityMode.DrawOnTop:
                        settings.visibilityMode = LabelVisibilityMode.Hidden;
                        break;
                    case LabelVisibilityMode.Hidden:
                        settings.visibilityMode = LabelVisibilityMode.Visible;
                        break;
                }
                
                // Update legacy enabled field for compatibility
                settings.enabled = settings.visibilityMode != LabelVisibilityMode.Hidden;
                
                LabelsOnFloorMod.Instance.WriteSettings(); // Save the change
                
                // Show a message to indicate the state change
                string messageKey = settings.visibilityMode switch
                {
                    LabelVisibilityMode.Visible => "FALCLF.LabelsVisible",
                    LabelVisibilityMode.DrawOnTop => "FALCLF.LabelsDrawOnTop",
                    LabelVisibilityMode.Hidden => "FALCLF.LabelsHidden",
                    _ => "FALCLF.LabelsVisible"
                };
                
                Messages.Message(messageKey.Translate(), MessageTypeDefOf.SilentInput, false);
            }
        }
    }
}