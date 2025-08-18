using System;
using UnityEngine;
using Verse;
using RimWorld;
using LabelsOnFloor.SettingsLibrary.Core;
using LabelsOnFloor.SettingsLibrary.Widgets;

namespace LabelsOnFloor.SettingsLibrary.Controls
{
    public class FontDropdownControl : SettingsItemBase
    {
        private readonly Func<string> _getter;
        private readonly Action<string> _setter;
        private const float ButtonWidth = 140f;  // Standardized width matching other dropdowns
        private const float ButtonHeight = 26f;
        
        // Performance optimization: Cache font lookups
        private string cachedFontName = null;
        private FontLibrary.IFont cachedFont = null;
        
        public FontDropdownControl(
            string id,
            string label,
            Func<string> getter,
            Action<string> setter,
            string tooltip = null) : base(id, label, tooltip)
        {
            _getter = getter ?? throw new ArgumentNullException(nameof(getter));
            _setter = setter ?? throw new ArgumentNullException(nameof(setter));
        }
        
        public override float Draw(float x, float y, float labelWidth, float controlWidth, ModSettingsConfiguration config)
        {
            if (!IsVisible())
                return 0f;
            
            float rowHeight = GetHeight();
            
            // Always draw label for layout consistency
            Rect labelRect = new Rect(x, y, labelWidth, rowHeight);
            DrawLabel(labelRect, label.Translate(), config);
            
            // Calculate button positions
            float refreshButtonSize = ButtonHeight; // Square refresh button
            float spacing = 4f;
            
            // Get current value
            string currentValue = _getter();
            
            // Update font cache if font changed
            if (currentValue != cachedFontName)
            {
                cachedFontName = currentValue;
                cachedFont = FontLibrary.FontRegistry.GetFont(currentValue);
                MarkDirty(); // Force redraw when font changes
            }
            
            // Draw refresh button (left of dropdown)
            Rect refreshRect = GetControlRect(x, y + (rowHeight - ButtonHeight) / 2f, labelWidth, controlWidth, 
                                            refreshButtonSize, ButtonHeight, config);
            refreshRect.x -= (ButtonWidth + spacing); // Position to the left of dropdown
            
            // Only redraw refresh button when needed
            if (NeedsRedraw(refreshRect, currentValue))
            {
                // Draw refresh icon/text
                if (Verse.Widgets.ButtonText(refreshRect, "↻", false))
                {
                    // Refresh the font registry
                    FontLibrary.FontRegistry.RefreshFonts();
                    Messages.Message("Fonts refreshed", MessageTypeDefOf.NeutralEvent, false);
                    cachedFontName = null; // Clear cache to force refresh
                    MarkDirty();
                }
                TooltipHandler.TipRegion(refreshRect, "Refresh font list");
            }
            
            // Draw dropdown button (original position)
            Rect buttonRect = GetControlRect(x, y + (rowHeight - ButtonHeight) / 2f, labelWidth, controlWidth, ButtonWidth, ButtonHeight, config);
            
            // Check if disabled by dependency
            bool wasEnabled = GUI.enabled;
            if (!IsEnabled())
            {
                GUI.enabled = false;
            }
            
            // Draw font dropdown with cached font to reduce lookups
            string currentFontName = _getter();
            FontDropdownWidget.ButtonDropdown(buttonRect, currentFontName, (newValue) =>
            {
                if (newValue != currentFontName)
                {
                    _setter(newValue);
                    cachedFontName = null; // Clear cache on change
                }
            });
            
            // Handle tooltip
            if (!string.IsNullOrEmpty(tooltip))
            {
                var fullRect = new Rect(x, y, labelWidth + controlWidth, rowHeight);
                TooltipHandler.TipRegion(fullRect, tooltip);
            }
            
            GUI.enabled = wasEnabled;
            
            return rowHeight;
        }
        
        public override float GetHeight()
        {
            return ModSettingsFramework.Defaults.RowHeight;
        }
    }
}