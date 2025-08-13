using System;
using UnityEngine;
using Verse;

namespace LabelsOnFloor.SettingsLibrary.Core
{
    /// <summary>
    /// Base class for all settings items with common functionality
    /// </summary>
    public abstract class SettingsItemBase : ISettingsItem
    {
        protected string id;
        protected string label;
        protected string tooltip;
        protected int indentLevel = 0;
        protected Func<bool> enabledCondition;
        protected Func<bool> visibilityCondition;
        
        protected SettingsItemBase(string id, string label, string tooltip = null)
        {
            this.id = id;
            this.label = label;
            this.tooltip = tooltip;
        }
        
        public string GetId() => id;
        public string GetLabel() => label;
        public string GetTooltip() => tooltip;
        public int GetIndentLevel() => indentLevel;
        public void SetIndentLevel(int level) => indentLevel = level;
        
        public abstract float Draw(float x, float y, float labelWidth, float controlWidth, ModSettingsConfiguration config);
        
        public virtual float GetHeight() => ModSettingsFramework.Defaults.RowHeight;
        
        public bool IsEnabled() => enabledCondition?.Invoke() ?? true;
        public bool IsVisible() => visibilityCondition?.Invoke() ?? true;
        
        public ISettingsItem DependsOn(Func<bool> condition)
        {
            enabledCondition = condition;
            return this;
        }
        
        public ISettingsItem SetVisibilityCondition(Func<bool> condition)
        {
            visibilityCondition = condition;
            return this;
        }
        
        /// <summary>
        /// Helper method to draw the label with proper alignment
        /// </summary>
        protected void DrawLabel(Rect labelRect, string text, ModSettingsConfiguration config)
        {
            // Labels are left-aligned
            Text.Anchor = TextAnchor.MiddleLeft;
            Verse.Widgets.Label(labelRect, text);
            
            // Add tooltip if present
            if (!string.IsNullOrEmpty(tooltip))
            {
                TooltipHandler.TipRegion(labelRect, tooltip.Translate());
            }
            
            Text.Anchor = TextAnchor.UpperLeft;
        }
        
        /// <summary>
        /// Helper to create properly aligned control rect (right-aligned in control column)
        /// </summary>
        protected Rect GetControlRect(float x, float y, float labelWidth, float controlWidth, float controlActualWidth, float height, ModSettingsConfiguration config)
        {
            // Right-align controls in their column
            float controlX = x + labelWidth + config.ControlPadding + (controlWidth - controlActualWidth);
            return new Rect(controlX, y, controlActualWidth, height);
        }
    }
}