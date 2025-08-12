using HugsLib.Settings;
using UnityEngine;
using Verse;
using System.Collections.Generic;

namespace LabelsOnFloor
{
    public class Dialog_DefaultLabelColorPicker : Window
    {
        private readonly SettingHandle<Color> _colorSetting;
        private Color selectedColor;
        
        // Predefined color palette - common text colors
        private static readonly List<Color> ColorPalette = new List<Color>
        {
            // Basic colors
            Color.white,
            Color.black,
            new Color(0.9f, 0.9f, 0.9f), // Light gray
            new Color(0.7f, 0.7f, 0.7f), // Medium gray
            new Color(0.5f, 0.5f, 0.5f), // Dark gray
            
            // Bright colors
            new Color(1f, 0.2f, 0.2f), // Red
            new Color(0.2f, 1f, 0.2f), // Green
            new Color(0.2f, 0.2f, 1f), // Blue
            new Color(1f, 1f, 0.2f), // Yellow
            new Color(1f, 0.2f, 1f), // Magenta
            new Color(0.2f, 1f, 1f), // Cyan
            new Color(1f, 0.6f, 0.2f), // Orange
            
            // Pastel colors
            new Color(1f, 0.7f, 0.7f), // Light red
            new Color(0.7f, 1f, 0.7f), // Light green
            new Color(0.7f, 0.7f, 1f), // Light blue
            new Color(1f, 1f, 0.7f), // Light yellow
            new Color(1f, 0.7f, 1f), // Light magenta
            new Color(0.7f, 1f, 1f), // Light cyan
            new Color(1f, 0.85f, 0.7f), // Light orange
            
            // Dark colors
            new Color(0.5f, 0f, 0f), // Dark red
            new Color(0f, 0.5f, 0f), // Dark green
            new Color(0f, 0f, 0.5f), // Dark blue
            new Color(0.5f, 0.5f, 0f), // Dark yellow
            new Color(0.5f, 0f, 0.5f), // Dark magenta
            new Color(0f, 0.5f, 0.5f), // Dark cyan
            new Color(0.5f, 0.3f, 0f), // Dark orange
        };
        
        private const int ColorButtonSize = 32;
        private const int ColorButtonPadding = 4;
        private const int ColorsPerRow = 7;
        
        public override Vector2 InitialSize => new Vector2(350f, 400f);
        
        public Dialog_DefaultLabelColorPicker(SettingHandle<Color> colorSetting)
        {
            _colorSetting = colorSetting;
            selectedColor = colorSetting.Value;
            
            forcePause = true;
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
        }
        
        public override void DoWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);
            
            // Title
            Text.Font = GameFont.Medium;
            listing.Label("FALCLF.ChooseDefaultLabelColor".Translate());
            Text.Font = GameFont.Small;
            
            listing.Gap(12f);
            
            // Current color preview
            listing.Label("FALCLF.CurrentColor".Translate() + ":");
            Rect colorPreviewRect = listing.GetRect(40f);
            Widgets.DrawBoxSolid(colorPreviewRect, selectedColor);
            Widgets.DrawBox(colorPreviewRect);
            
            listing.Gap(12f);
            
            // Color palette
            listing.Label("FALCLF.SelectColor".Translate() + ":");
            Rect paletteRect = listing.GetRect((ColorButtonSize + ColorButtonPadding) * ((ColorPalette.Count - 1) / ColorsPerRow + 1));
            DrawColorPalette(paletteRect);
            
            listing.End();
            
            // Bottom buttons
            float buttonY = inRect.height - 35f;
            float buttonWidth = 100f;
            
            // Reset to white button (left)
            if (Widgets.ButtonText(new Rect(0f, buttonY, buttonWidth, 30f), "FALCLF.Reset".Translate()))
            {
                selectedColor = Color.white;
            }
            
            // Cancel button (center-right)
            if (Widgets.ButtonText(new Rect(inRect.width - buttonWidth * 2 - 10f, buttonY, buttonWidth, 30f), "FALCLF.Cancel".Translate()))
            {
                Close();
            }
            
            // OK button (right)
            if (Widgets.ButtonText(new Rect(inRect.width - buttonWidth, buttonY, buttonWidth, 30f), "FALCLF.OK".Translate()))
            {
                _colorSetting.Value = selectedColor;
                Close();
            }
        }
        
        private void DrawColorPalette(Rect rect)
        {
            float x = rect.x;
            float y = rect.y;
            int col = 0;
            
            foreach (var color in ColorPalette)
            {
                Rect buttonRect = new Rect(x, y, ColorButtonSize, ColorButtonSize);
                
                // Draw color button
                Widgets.DrawBoxSolid(buttonRect, color);
                
                // Highlight selected color
                if (ColorsAreClose(selectedColor, color))
                {
                    Widgets.DrawBox(buttonRect, 3);
                }
                else
                {
                    Widgets.DrawBox(buttonRect);
                }
                
                // Handle click
                if (Widgets.ButtonInvisible(buttonRect))
                {
                    selectedColor = color;
                }
                
                // Move to next position
                col++;
                if (col >= ColorsPerRow)
                {
                    col = 0;
                    x = rect.x;
                    y += ColorButtonSize + ColorButtonPadding;
                }
                else
                {
                    x += ColorButtonSize + ColorButtonPadding;
                }
            }
        }
        
        private bool ColorsAreClose(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.01f && 
                   Mathf.Abs(a.g - b.g) < 0.01f && 
                   Mathf.Abs(a.b - b.b) < 0.01f;
        }
    }
}