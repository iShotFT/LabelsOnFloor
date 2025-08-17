using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using LabelsOnFloor.SettingsLibrary.Controls;

namespace LabelsOnFloor.SettingsLibrary.Core
{
    /// <summary>
    /// Implementation of a settings category that holds multiple settings items
    /// </summary>
    public class SettingsCategory : ISettingsCategory
    {
        private readonly string label;
        private readonly string description;
        private readonly List<ISettingsItem> items = new List<ISettingsItem>();
        private Func<bool> visibilityCondition;
        private readonly ModSettingsConfiguration config;
        
        public SettingsCategory(string label, string description, ModSettingsConfiguration config)
        {
            this.label = label;
            this.description = description;
            this.config = config;
        }
        
        public string GetLabel() => label;
        public string GetDescription() => description;
        public List<ISettingsItem> GetItems() => items;
        
        public bool IsVisible() => visibilityCondition?.Invoke() ?? true;
        
        public ISettingsCategory SetVisibilityCondition(Func<bool> condition)
        {
            visibilityCondition = condition;
            return this;
        }
        
        public ISettingsItem AddCheckbox(string id, string labelKey, Func<bool> getter, Action<bool> setter, string tooltipKey = null)
        {
            var item = new CheckboxControl(id, labelKey, getter, setter, tooltipKey);
            items.Add(item);
            return item;
        }
        
        public ISettingsItem AddSlider(string id, string labelKey, Func<float> getter, Action<float> setter, 
            float min, float max, string tooltipKey = null, Func<float, string> valueFormatter = null)
        {
            var item = new SliderControl(id, labelKey, getter, setter, min, max, tooltipKey, valueFormatter);
            items.Add(item);
            return item;
        }
        
        public ISettingsItem AddIntSlider(string id, string labelKey, Func<int> getter, Action<int> setter,
            int min, int max, string tooltipKey = null, Func<int, string> valueFormatter = null)
        {
            // Convert int to float for the slider
            Func<float, string> floatFormatter = valueFormatter != null 
                ? (f => valueFormatter(Mathf.RoundToInt(f)))
                : (Func<float, string>)null;
                
            var item = new SliderControl(id, labelKey, 
                () => getter(), 
                v => setter(Mathf.RoundToInt(v)), 
                min, max, tooltipKey, floatFormatter);
            items.Add(item);
            return item;
        }
        
        public ISettingsItem AddColorPicker(string id, string labelKey, Func<Color> getter, Action<Color> setter, string tooltipKey = null)
        {
            var item = new ColorPickerControl(id, labelKey, getter, setter, tooltipKey);
            items.Add(item);
            return item;
        }
        
        public ISettingsItem AddDropdown<T>(string id, string labelKey, Func<T> getter, Action<T> setter, 
            IEnumerable<T> options, Func<T, string> optionLabelGetter, string tooltipKey = null)
        {
            var item = new DropdownControl<T>(id, labelKey, getter, setter, options, optionLabelGetter, tooltipKey);
            items.Add(item);
            return item;
        }
        
        public ISettingsItem AddSliderDropdown(string id, string labelKey, Func<float> getter, Action<float> setter,
            float min, float max, string tooltipKey = null, Func<float, string> valueFormatter = null)
        {
            var item = new SliderDropdownControl(id, labelKey, getter, setter, min, max, tooltipKey, valueFormatter);
            items.Add(item);
            return item;
        }
        
        public ISettingsItem AddIntSliderDropdown(string id, string labelKey, Func<int> getter, Action<int> setter,
            int min, int max, string tooltipKey = null, Func<int, string> valueFormatter = null)
        {
            // Convert int to float for the slider
            Func<float, string> floatFormatter = valueFormatter != null 
                ? (f => valueFormatter(Mathf.RoundToInt(f)))
                : (Func<float, string>)null;
                
            var item = new SliderDropdownControl(id, labelKey, 
                () => getter(), 
                v => setter(Mathf.RoundToInt(v)), 
                min, max, tooltipKey, floatFormatter);
            items.Add(item);
            return item;
        }
        
        public void AddGap(float height = 12f)
        {
            items.Add(new GapControl(height));
        }
        
        public void AddSeparator()
        {
            items.Add(new SeparatorControl());
        }
        
        public ISettingsItem AddControl(ISettingsItem control)
        {
            items.Add(control);
            return control;
        }
    }
}