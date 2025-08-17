using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace LabelsOnFloor.Patches
{
    /// <summary>
    /// Patch to intercept when the rename button is clicked for zones in the inspect pane
    /// </summary>
    [HarmonyPatch(typeof(RenameUIUtility), "DrawRenameButton")]
    [HarmonyPatch(new[] { typeof(Rect), typeof(IRenameable) })]
    public static class RenameUIUtility_DrawRenameButton_Patch
    {
        public static bool Prefix(Rect rect, IRenameable renamable)
        {
            // Check if this is a Zone
            if (renamable is Zone zone)
            {
                // Draw the button ourselves
                TooltipHandler.TipRegionByKey(rect, "Rename");
                if (Widgets.ButtonImage(rect, TexButton.Rename))
                {
                    // Use our custom dialog instead
                    Find.WindowStack.Add(new Dialog_RenameZoneWithColor(zone));
                }
                
                // Skip the original method
                return false;
            }
            
            // Let other renameables use the default behavior
            return true;
        }
    }
}