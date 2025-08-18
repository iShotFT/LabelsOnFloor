using System;
using UnityEngine;
using Verse;
using LabelsOnFloor.SettingsLibrary.Core;

namespace LabelsOnFloor.SettingsLibrary.Controls
{
    /// <summary>
    /// Checkbox control with proper two-column layout
    /// </summary>
    public class CheckboxControl : SettingsItemBase
    {
        private readonly Func<bool> getter;
        private readonly Action<bool> setter;
        private const float CheckboxSize = 24f;
        
        public CheckboxControl(string id, string label, Func<bool> getter, Action<bool> setter, string tooltip = null)
            : base(id, label, tooltip)
        {
            this.getter = getter;
            this.setter = setter;
        }
        
        public override float Draw(float x, float y, float labelWidth, float controlWidth, ModSettingsConfiguration config)
        {
            float rowHeight = GetHeight();
            
            // Draw label on the left
            Rect labelRect = new Rect(x, y, labelWidth, rowHeight);
            DrawLabel(labelRect, label.Translate(), config);
            
            // Draw checkbox on the right (right-aligned in control column)
            Rect checkboxRect = GetControlRect(x, y + (rowHeight - CheckboxSize) / 2f, labelWidth, controlWidth, CheckboxSize, CheckboxSize, config);
            
            bool value = getter();
            bool oldValue = value;
            
            // We must always call Checkbox as it handles both rendering and input
            Verse.Widgets.Checkbox(checkboxRect.position, ref value);
            
            if (value != oldValue)
            {
                setter(value);
            }
            
            return rowHeight;
        }
    }
}