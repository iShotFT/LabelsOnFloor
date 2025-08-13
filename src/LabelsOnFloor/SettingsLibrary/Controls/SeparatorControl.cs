using UnityEngine;
using Verse;
using LabelsOnFloor.SettingsLibrary.Core;

namespace LabelsOnFloor.SettingsLibrary.Controls
{
    /// <summary>
    /// Horizontal line separator control
    /// </summary>
    public class SeparatorControl : SettingsItemBase
    {
        public SeparatorControl() : base("separator", "") { }
        
        public override float Draw(float x, float y, float labelWidth, float controlWidth, ModSettingsConfiguration config)
        {
            float totalWidth = labelWidth + controlWidth + config.ControlPadding;
            Color lineColor = new Color(1f, 1f, 1f, 0.2f);
            Verse.Widgets.DrawLineHorizontal(x, y + 6f, totalWidth, lineColor);
            return 12f;
        }
        
        public override float GetHeight() => 12f;
    }
}