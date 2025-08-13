using UnityEngine;
using LabelsOnFloor.SettingsLibrary.Core;

namespace LabelsOnFloor.SettingsLibrary.Controls
{
    /// <summary>
    /// Simple gap/spacer control
    /// </summary>
    public class GapControl : SettingsItemBase
    {
        private readonly float height;
        
        public GapControl(float height) : base("gap", "")
        {
            this.height = height;
        }
        
        public override float Draw(float x, float y, float labelWidth, float controlWidth, ModSettingsConfiguration config)
        {
            return height;
        }
        
        public override float GetHeight() => height;
    }
}