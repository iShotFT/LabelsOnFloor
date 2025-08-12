using RimWorld;
using Verse;

namespace LabelsOnFloor
{
    [DefOf]
    public static class KeyBindingDefOf_LabelsOnFloor
    {
        public static KeyBindingDef LabelsOnFloor_RenameRoom;
        public static KeyBindingDef LabelsOnFloor_ToggleLabels;
        
        static KeyBindingDefOf_LabelsOnFloor()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(KeyBindingDefOf_LabelsOnFloor));
        }
    }
}