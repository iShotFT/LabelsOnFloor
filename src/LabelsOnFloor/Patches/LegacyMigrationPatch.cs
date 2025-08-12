using HarmonyLib;
using RimWorld.Planet;

namespace LabelsOnFloor.Patches
{
    // Patch to trigger migration after world is fully loaded
    [HarmonyPatch(typeof(World), "FinalizeInit")]
    public static class WorldFinalizeInitPatch
    {
        static void Postfix()
        {
            // Process any pending migrations from legacy save format
            MigrationHelper.ProcessMigrations();
        }
    }
}