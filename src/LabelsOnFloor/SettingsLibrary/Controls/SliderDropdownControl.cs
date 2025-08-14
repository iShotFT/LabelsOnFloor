using System;
using UnityEngine;
using Verse;
using RimWorld;
using LabelsOnFloor.SettingsLibrary.Core;
using Verse.Sound;

namespace LabelsOnFloor.SettingsLibrary.Controls
{
    /// <summary>
    /// Innovative slider-in-dropdown control - displays value as a button that opens a dropdown with slider
    /// </summary>
    public class SliderDropdownControl : SettingsItemBase
    {
        private readonly Func<float> getter;
        private readonly Action<float> setter;
        private readonly float min;
        private readonly float max;
        private readonly Func<float, string> valueFormatter;
        private const float ButtonWidth = 140f;  // Standardized width across all controls
        private const float ButtonHeight = 26f;
        
        public SliderDropdownControl(string id, string label, Func<float> getter, Action<float> setter,
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
            
            // Draw label on the left
            Rect labelRect = new Rect(x, y, labelWidth, rowHeight);
            DrawLabel(labelRect, label.Translate(), config);
            
            // Draw dropdown button on the right (right-aligned)
            Rect buttonRect = GetControlRect(x, y + (rowHeight - ButtonHeight) / 2f, labelWidth, controlWidth, ButtonWidth, ButtonHeight, config);
            
            // Draw button background
            Verse.Widgets.DrawAtlas(buttonRect, TexUI.FloatMenuOptionBG);
            
            // Highlight on hover
            if (Mouse.IsOver(buttonRect))
            {
                Verse.Widgets.DrawHighlight(buttonRect);
            }
            
            // Draw current value
            string valueText = valueFormatter != null ? valueFormatter(value) : value.ToString("F1");
            Text.Anchor = TextAnchor.MiddleCenter;
            Verse.Widgets.Label(buttonRect, valueText);
            
            // Draw dropdown arrow on the right side
            Text.Font = GameFont.Small;
            Rect arrowRect = new Rect(buttonRect.xMax - 20f, buttonRect.y, 20f, buttonRect.height);
            Verse.Widgets.Label(arrowRect, "▼");
            Text.Anchor = TextAnchor.UpperLeft;
            
            // Handle click to open slider dropdown
            if (Verse.Widgets.ButtonInvisible(buttonRect))
            {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                Find.WindowStack.Add(new SliderDropdownWindow(
                    value,
                    min,
                    max,
                    newValue => {
                        setter(newValue);
                    },
                    valueFormatter
                ));
            }
            
            return rowHeight;
        }
    }
    
    /// <summary>
    /// Custom dropdown window that contains a slider
    /// </summary>
    public class SliderDropdownWindow : Window
    {
        private float currentValue;
        private readonly float min;
        private readonly float max;
        private readonly Action<float> onValueChanged;
        private readonly Func<float, string> valueFormatter;
        private readonly float originalValue;
        
        private const float WindowWidth = 260f;
        private const float WindowHeight = 50f;  // Even more compact
        private const float Padding = 6f;
        
        public override Vector2 InitialSize => new Vector2(WindowWidth, WindowHeight);
        
        protected override float Margin => 0f;
        
        public SliderDropdownWindow(float currentValue, float min, float max, 
            Action<float> onValueChanged, Func<float, string> valueFormatter)
        {
            this.currentValue = currentValue;
            this.originalValue = currentValue;
            this.min = min;
            this.max = max;
            this.onValueChanged = onValueChanged;
            this.valueFormatter = valueFormatter;
            
            // Window settings
            layer = WindowLayer.Super;
            closeOnClickedOutside = true;
            doWindowBackground = false;
            drawShadow = false;
            preventCameraMotion = false;
            
            SoundDefOf.FloatMenu_Open.PlayOneShotOnCamera();
        }
        
        protected override void SetInitialSizeAndPosition()
        {
            // Position at mouse cursor
            Vector2 mousePos = UI.MousePositionOnUIInverted;
            
            // Ensure window stays on screen
            if (mousePos.x + InitialSize.x > UI.screenWidth)
            {
                mousePos.x = UI.screenWidth - InitialSize.x;
            }
            if (mousePos.y + InitialSize.y > UI.screenHeight)
            {
                mousePos.y = UI.screenHeight - InitialSize.y;
            }
            
            windowRect = new Rect(mousePos.x, mousePos.y, InitialSize.x, InitialSize.y);
        }
        
        public override void DoWindowContents(Rect inRect)
        {
            // Draw background
            GUI.color = new Color(0.13f, 0.13f, 0.13f);
            GUI.DrawTexture(inRect, BaseContent.WhiteTex);
            Verse.Widgets.DrawAtlas(inRect, TexUI.FloatMenuOptionBG);
            GUI.color = Color.white;
            
            Rect contentRect = inRect.ContractedBy(Padding);
            
            // Draw value on the LEFT of the slider with smaller text
            string valueText = valueFormatter != null ? valueFormatter(currentValue) : currentValue.ToString("F1");
            float valueWidth = 50f;
            Rect valueRect = new Rect(contentRect.x, contentRect.y, valueWidth, contentRect.height);
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Tiny; // Smaller text size
            Verse.Widgets.Label(valueRect, valueText);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            
            // Draw slider to the right of the value
            float sliderX = valueRect.xMax + 8f;
            Rect sliderRect = new Rect(sliderX, contentRect.y + (contentRect.height - 22f) / 2f, contentRect.width - valueWidth - 8f, 22f);
            float newValue = Verse.Widgets.HorizontalSlider(sliderRect, currentValue, min, max);
            
            if (Math.Abs(newValue - currentValue) > 0.001f)
            {
                currentValue = newValue;
                onValueChanged(currentValue);
            }
            
            // Min/max labels removed for compactness - the slider shows them visually
            
            // Handle escape to cancel
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                onValueChanged(originalValue);
                Close();
                Event.current.Use();
            }
        }
        
        public override void PostClose()
        {
            base.PostClose();
            SoundDefOf.Click.PlayOneShotOnCamera();
        }
    }
}