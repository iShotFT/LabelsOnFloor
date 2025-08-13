using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;
using LabelsOnFloor.SettingsLibrary.Core;

namespace LabelsOnFloor.SettingsLibrary.Controls
{
    /// <summary>
    /// Dropdown control with proper right-alignment
    /// </summary>
    public class DropdownControl<T> : SettingsItemBase
    {
        private readonly Func<T> getter;
        private readonly Action<T> setter;
        private readonly IEnumerable<T> options;
        private readonly Func<T, string> optionLabelGetter;
        private const float ButtonWidth = 140f;  // Standardized width across all controls
        private const float ButtonHeight = 26f;
        
        public DropdownControl(string id, string label, Func<T> getter, Action<T> setter,
            IEnumerable<T> options, Func<T, string> optionLabelGetter, string tooltip = null)
            : base(id, label, tooltip)
        {
            this.getter = getter;
            this.setter = setter;
            this.options = options;
            this.optionLabelGetter = optionLabelGetter;
        }
        
        public override float Draw(float x, float y, float labelWidth, float controlWidth, ModSettingsConfiguration config)
        {
            float rowHeight = GetHeight();
            
            // Draw label on the left
            Rect labelRect = new Rect(x, y, labelWidth, rowHeight);
            DrawLabel(labelRect, label.Translate(), config);
            
            // Draw dropdown button on the right (right-aligned)
            Rect dropdownRect = GetControlRect(x, y + (rowHeight - ButtonHeight) / 2f, labelWidth, controlWidth, ButtonWidth, ButtonHeight, config);
            
            T currentValue = getter();
            string currentLabel = optionLabelGetter(currentValue);
            
            // Draw dropdown button background
            Verse.Widgets.DrawAtlas(dropdownRect, TexUI.FloatMenuOptionBG);
            
            // Highlight on hover
            if (Mouse.IsOver(dropdownRect))
            {
                Verse.Widgets.DrawHighlight(dropdownRect);
            }
            
            // Draw current value text
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect textRect = new Rect(dropdownRect.x + 6f, dropdownRect.y, dropdownRect.width - 30f, dropdownRect.height);
            Verse.Widgets.Label(textRect, currentLabel);
            
            // Draw dropdown arrow
            Text.Anchor = TextAnchor.MiddleCenter;
            Rect arrowRect = new Rect(dropdownRect.xMax - 25f, dropdownRect.y, 20f, dropdownRect.height);
            Verse.Widgets.Label(arrowRect, "▼");
            Text.Anchor = TextAnchor.UpperLeft;
            
            // Handle click
            if (Verse.Widgets.ButtonInvisible(dropdownRect))
            {
                List<FloatMenuOption> menuOptions = new List<FloatMenuOption>();
                foreach (T option in options)
                {
                    T localOption = option; // Capture for closure
                    menuOptions.Add(new FloatMenuOption(
                        optionLabelGetter(localOption),
                        () => setter(localOption)
                    ));
                }
                Find.WindowStack.Add(new FloatMenu(menuOptions));
            }
            
            return rowHeight;
        }
    }
}