using System;
using UnityEngine;

namespace LabelsOnFloor.SettingsLibrary.Core
{
    /// <summary>
    /// Interface for all settings items
    /// </summary>
    public interface ISettingsItem
    {
        string GetId();
        string GetLabel();
        string GetTooltip();
        int GetIndentLevel();
        void SetIndentLevel(int level);
        
        /// <summary>
        /// Draw this item and return the height used
        /// </summary>
        float Draw(float x, float y, float labelWidth, float controlWidth, ModSettingsConfiguration config);
        
        /// <summary>
        /// Get the height this item will use
        /// </summary>
        float GetHeight();
        
        /// <summary>
        /// Check if this item should be enabled
        /// </summary>
        bool IsEnabled();
        
        /// <summary>
        /// Check if this item should be visible
        /// </summary>
        bool IsVisible();
        
        /// <summary>
        /// Set a dependency condition
        /// </summary>
        ISettingsItem DependsOn(Func<bool> condition);
        
        /// <summary>
        /// Set visibility condition
        /// </summary>
        ISettingsItem SetVisibilityCondition(Func<bool> condition);
    }
}