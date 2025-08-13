using System;
using System.Collections.Generic;
using UnityEngine;

namespace LabelsOnFloor.SettingsLibrary.Core
{
    /// <summary>
    /// Interface for settings categories
    /// </summary>
    public interface ISettingsCategory
    {
        string GetLabel();
        string GetDescription();
        List<ISettingsItem> GetItems();
        bool IsVisible();
        ISettingsCategory SetVisibilityCondition(Func<bool> condition);
        
        // Methods to add different types of settings
        ISettingsItem AddCheckbox(string id, string labelKey, Func<bool> getter, Action<bool> setter, string tooltipKey = null);
        ISettingsItem AddSlider(string id, string labelKey, Func<float> getter, Action<float> setter, float min, float max, string tooltipKey = null, Func<float, string> valueFormatter = null);
        ISettingsItem AddIntSlider(string id, string labelKey, Func<int> getter, Action<int> setter, int min, int max, string tooltipKey = null, Func<int, string> valueFormatter = null);
        ISettingsItem AddColorPicker(string id, string labelKey, Func<Color> getter, Action<Color> setter, string tooltipKey = null);
        ISettingsItem AddDropdown<T>(string id, string labelKey, Func<T> getter, Action<T> setter, IEnumerable<T> options, Func<T, string> optionLabelGetter, string tooltipKey = null);
        ISettingsItem AddSliderDropdown(string id, string labelKey, Func<float> getter, Action<float> setter, float min, float max, string tooltipKey = null, Func<float, string> valueFormatter = null);
        ISettingsItem AddIntSliderDropdown(string id, string labelKey, Func<int> getter, Action<int> setter, int min, int max, string tooltipKey = null, Func<int, string> valueFormatter = null);
        void AddGap(float height = 12f);
        void AddSeparator();
    }
}