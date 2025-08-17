using HarmonyLib;
using RimWorld;
using Verse;

namespace LabelsOnFloor.Patches
{
    /// <summary>
    /// Patch to intercept Dialog_RenameZone construction and redirect to our custom dialog
    /// This catches any code that tries to create the vanilla rename zone dialog
    /// </summary>
    [HarmonyPatch(typeof(Dialog_RenameZone), MethodType.Constructor)]
    [HarmonyPatch(new[] { typeof(Zone) })]
    public static class Dialog_RenameZone_Constructor_Patch
    {
        public static bool Prefix(Zone zone)
        {
            // Prevent the vanilla dialog from being created
            // Instead, create our custom dialog with color picker
            // This automatically gets or creates the CustomZoneData
            Find.WindowStack.Add(new Dialog_RenameZoneWithColor(zone));
            
            // Skip the original constructor
            return false;
        }
    }
}