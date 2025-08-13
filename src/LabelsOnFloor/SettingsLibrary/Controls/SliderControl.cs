using System;
using UnityEngine;
using Verse;
using LabelsOnFloor.SettingsLibrary.Core;

namespace LabelsOnFloor.SettingsLibrary.Controls
{
    /// <summary>
    /// Slider control with value display on the right
    /// </summary>
    public class SliderControl : SettingsItemBase
    {
        private readonly Func<float> getter;
        private readonly Action<float> setter;
        private readonly float min;
        private readonly float max;
        private readonly Func<float, string> valueFormatter;
        private const float SliderHeight = 22f;
        private const float ValueBoxWidth = 60f;
        private const float ValueBoxPadding = 8f;
        
        public SliderControl(string id, string label, Func<float> getter, Action<float> setter,
            float min, float max, string tooltip = null, Func<float, string> valueFormatter = null)
            : base(id, label, tooltip)
        {
            this.getter = getter;
            this.setter = setter;
            this.min = min;
            this.max = max;
            this.valueFormatter = valueFormatter;
        }
        
        public override float Draw(float x, float y, float labelWidth, float controlWidth, ModSettingsConfiguration config)
        {
            float rowHeight = GetHeight();
            float value = getter();
            
            // Draw label on the left (without value)
            Rect labelRect = new Rect(x, y, labelWidth, rowHeight);
            DrawLabel(labelRect, label.Translate(), config);
            
            // Calculate slider and value box sizes
            float sliderWidth = controlWidth - ValueBoxWidth - ValueBoxPadding;
            
            // Draw slider (left part of control column)
            float sliderX = x + labelWidth + config.ControlPadding;
            Rect sliderRect = new Rect(sliderX, y + (rowHeight - SliderHeight) / 2f, sliderWidth, SliderHeight);
            float newValue = Verse.Widgets.HorizontalSlider(sliderRect, value, min, max);
            
            if (Math.Abs(newValue - value) > 0.001f)
            {
                setter(newValue);
                value = newValue;
            }
            
            // Draw value box on the right (right-aligned)
            string valueText = valueFormatter != null ? valueFormatter(value) : value.ToString("F1");
            Rect valueBoxRect = new Rect(
                x + labelWidth + config.ControlPadding + controlWidth - ValueBoxWidth,
                y + (rowHeight - 24f) / 2f,
                ValueBoxWidth,
                24f
            );
            
            // Draw value box background
            Verse.Widgets.DrawAtlas(valueBoxRect, TexUI.FloatMenuOptionBG);
            
            // Draw value text centered
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.white;
            Verse.Widgets.Label(valueBoxRect, valueText);
            Text.Anchor = TextAnchor.UpperLeft;
            
            return rowHeight;
        }
    }
}