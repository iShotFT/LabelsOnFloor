using System;
using UnityEngine;
using Verse;
using LabelsOnFloor.SettingsLibrary.Core;
using LabelsOnFloor.SettingsLibrary.Widgets;

namespace LabelsOnFloor.SettingsLibrary.Controls
{
    /// <summary>
    /// Color picker control using the existing ColorDropdownWidget
    /// </summary>
    public class ColorPickerControl : SettingsItemBase
    {
        private readonly Func<Color> getter;
        private readonly Action<Color> setter;
        private const float ButtonWidth = 140f;  // Standardized width across all controls
        private const float ButtonHeight = 26f;
        
        public ColorPickerControl(string id, string label, Func<Color> getter, Action<Color> setter, string tooltip = null)
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
            
            // Draw color picker on the right (right-aligned)
            Rect pickerRect = GetControlRect(x, y + (rowHeight - ButtonHeight) / 2f, labelWidth, controlWidth, ButtonWidth, ButtonHeight, config);
            
            Color currentColor = getter();
            Color? nullableColor = currentColor;
            
            // Use the minimal color dropdown widget (no RGB text)
            ColorDropdownWidget.DrawMinimalColorDropdownButton(
                pickerRect,
                ref nullableColor,
                Color.white,
                (color) => {
                    if (color.HasValue)
                    {
                        setter(color.Value);
                    }
                },
                tooltip,  // Pass through the tooltip
                config.DefaultColorLabelKey  // Pass the translation key for default color
            );
            
            if (nullableColor.HasValue && nullableColor.Value != currentColor)
            {
                setter(nullableColor.Value);
            }
            
            return rowHeight;
        }
    }
}