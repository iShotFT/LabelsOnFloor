using UnityEngine;
using Verse;

namespace LabelsOnFloor
{
    [StaticConstructorOnStartup]
    public class Resources
    {
        // Note: Font loading is now handled in FontHandler.cs to support extended character set
        public static Texture2D Rename = ContentFinder<Texture2D>.Get("Rename");
    }
}