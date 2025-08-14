using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace LabelsOnFloor
{
    public class Dialog_RoomColorPicker : Window
    {
        private Action<Color> onColorSelected;
        private Color currentColor;
        
        // Predefined color palette similar to RimWorld's area colors
        private static readonly List<Color> ColorPalette = new List<Color>
        {
            // Grays and whites
            Color.white,
            new Color(0.9f, 0.9f, 0.9f),
            new Color(0.7f, 0.7f, 0.7f),
            new Color(0.5f, 0.5f, 0.5f),
            new Color(0.3f, 0.3f, 0.3f),
            Color.black,
            
            // Reds
            new Color(1f, 0.2f, 0.2f),
            new Color(0.8f, 0.2f, 0.2f),
            new Color(0.9f, 0.4f, 0.4f),
            new Color(1f, 0.6f, 0.6f),
            new Color(0.6f, 0.2f, 0.2f),
            new Color(0.4f, 0.1f, 0.1f),
            
            // Oranges
            new Color(1f, 0.5f, 0.2f),
            new Color(1f, 0.6f, 0.3f),
            new Color(1f, 0.7f, 0.4f),
            new Color(0.9f, 0.5f, 0.1f),
            new Color(0.8f, 0.4f, 0.1f),
            new Color(0.6f, 0.3f, 0.1f),
            
            // Yellows
            new Color(1f, 1f, 0.2f),
            new Color(0.9f, 0.9f, 0.2f),
            new Color(1f, 1f, 0.4f),
            new Color(1f, 1f, 0.6f),
            new Color(0.8f, 0.8f, 0.4f),
            new Color(0.6f, 0.6f, 0.2f),
            
            // Greens
            new Color(0.2f, 1f, 0.2f),
            new Color(0.2f, 0.8f, 0.2f),
            new Color(0.4f, 0.9f, 0.4f),
            new Color(0.6f, 1f, 0.6f),
            new Color(0.1f, 0.6f, 0.1f),
            new Color(0.1f, 0.4f, 0.1f),
            
            // Cyans
            new Color(0.2f, 1f, 1f),
            new Color(0.2f, 0.8f, 0.8f),
            new Color(0.4f, 0.9f, 0.9f),
            new Color(0.6f, 1f, 1f),
            new Color(0.1f, 0.6f, 0.6f),
            new Color(0.1f, 0.4f, 0.4f),
            
            // Blues
            new Color(0.2f, 0.2f, 1f),
            new Color(0.2f, 0.2f, 0.8f),
            new Color(0.4f, 0.4f, 0.9f),
            new Color(0.6f, 0.6f, 1f),
            new Color(0.1f, 0.1f, 0.6f),
            new Color(0.1f, 0.1f, 0.4f),
            
            // Purples
            new Color(0.8f, 0.2f, 1f),
            new Color(0.6f, 0.2f, 0.8f),
            new Color(0.7f, 0.4f, 0.9f),
            new Color(0.8f, 0.6f, 1f),
            new Color(0.4f, 0.1f, 0.6f),
            new Color(0.3f, 0.1f, 0.4f),
            
            // Pinks
            new Color(1f, 0.2f, 0.8f),
            new Color(0.9f, 0.2f, 0.6f),
            new Color(1f, 0.4f, 0.7f),
            new Color(1f, 0.6f, 0.8f),
            new Color(0.6f, 0.1f, 0.4f),
            new Color(0.8f, 0.3f, 0.5f)
        };
        
        private const int ColorButtonSize = 30;
        private const int ColorButtonPadding = 5;
        private const int ColorsPerRow = 12;
        
        public override Vector2 InitialSize => new Vector2(450f, 350f);
        
        public Dialog_RoomColorPicker(Action<Color> onColorSelected, Color initialColor)
        {
            this.onColorSelected = onColorSelected;
            this.currentColor = initialColor;
            
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
            listing.Label("FALCLF.ChooseColor".Translate());
            Text.Font = GameFont.Small;
            
            listing.Gap(12f);
            
            // Current color preview
            listing.Label("FALCLF.CurrentColor".Translate());
            Rect colorPreviewRect = listing.GetRect(40f);
            Verse.Widgets.DrawBoxSolid(colorPreviewRect, currentColor);
            Verse.Widgets.DrawBox(colorPreviewRect);
            
            listing.Gap(12f);
            listing.Label("FALCLF.SelectFromPalette".Translate());
            listing.Gap(8f);
            
            // Color palette grid
            int rows = (ColorPalette.Count - 1) / ColorsPerRow + 1;
            Rect paletteRect = listing.GetRect((ColorButtonSize + ColorButtonPadding) * rows);
            DrawColorPalette(paletteRect);
            
            listing.End();
            
            // Bottom buttons
            float buttonY = inRect.height - 35f;
            float buttonWidth = 120f;
            
            // Cancel button (left)
            if (Verse.Widgets.ButtonText(new Rect(0f, buttonY, buttonWidth, 30f), "FALCLF.Cancel".Translate()))
            {
                Close();
            }
            
            // OK button (right)
            if (Verse.Widgets.ButtonText(new Rect(inRect.width - buttonWidth, buttonY, buttonWidth, 30f), "FALCLF.OK".Translate()))
            {
                onColorSelected?.Invoke(currentColor);
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
                Verse.Widgets.DrawBoxSolid(buttonRect, color);
                
                // Highlight selected color
                if (ColorsAreClose(currentColor, color))
                {
                    Verse.Widgets.DrawBox(buttonRect, 3);
                }
                else
                {
                    Verse.Widgets.DrawBox(buttonRect);
                }
                
                // Handle click
                if (Verse.Widgets.ButtonInvisible(buttonRect))
                {
                    currentColor = color;
                }
                
                // Tooltip with color values
                if (Mouse.IsOver(buttonRect))
                {
                    TooltipHandler.TipRegion(buttonRect, $"RGB: ({color.r:F2}, {color.g:F2}, {color.b:F2})");
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