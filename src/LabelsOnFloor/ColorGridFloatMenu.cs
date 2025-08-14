using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace LabelsOnFloor
{
    /// <summary>
    /// Custom FloatMenu that displays a grid of color squares for selection
    /// Based on RimWorld's FloatMenu but customized for color picking
    /// </summary>
    public class ColorGridFloatMenu : Window
    {
        private List<Color?> colors;
        private Color? selectedColor;
        private Action<Color?> onColorSelected;
        
        // Grid configuration
        private const int ColorsPerRow = 5;
        private const float ColorSquareSize = 32f;
        private const float ColorSpacing = 2f;
        private const float MenuPadding = 4f;
        
        // Visual settings from FloatMenu
        private static readonly Color ColorBGActive = new ColorInt(21, 25, 29).ToColor;
        private static readonly Color ColorBGHover = new ColorInt(29, 45, 50).ToColor;
        private static readonly Vector2 InitialPositionShift = new Vector2(4f, 0f);
        
        // Calculated dimensions
        private int rows;
        private float menuWidth;
        private float menuHeight;
        
        public override Vector2 InitialSize => new Vector2(menuWidth, menuHeight);
        
        protected override float Margin => 0f;
        
        public ColorGridFloatMenu(List<Color?> colorOptions, Color? currentSelection, Action<Color?> onSelection)
        {
            colors = colorOptions;
            selectedColor = currentSelection;
            onColorSelected = onSelection;
            
            // Calculate menu dimensions
            rows = (colors.Count - 1) / ColorsPerRow + 1;
            menuWidth = (ColorsPerRow * ColorSquareSize) + ((ColorsPerRow - 1) * ColorSpacing) + (MenuPadding * 2);
            menuHeight = (rows * ColorSquareSize) + ((rows - 1) * ColorSpacing) + (MenuPadding * 2);
            
            // Window settings from FloatMenu
            layer = WindowLayer.Super;
            closeOnClickedOutside = true;
            doWindowBackground = false;
            drawShadow = false;
            preventCameraMotion = false;
            
            // Play menu open sound
            SoundDefOf.FloatMenu_Open.PlayOneShotOnCamera();
        }
        
        protected override void SetInitialSizeAndPosition()
        {
            // Position at mouse cursor like FloatMenu
            Vector2 mousePos = UI.MousePositionOnUIInverted + InitialPositionShift;
            
            // Ensure menu stays on screen
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
            GUI.color = ColorBGActive;
            GUI.DrawTexture(inRect, BaseContent.WhiteTex);
            Verse.Widgets.DrawAtlas(inRect, TexUI.FloatMenuOptionBG);
            GUI.color = Color.white;
            
            // Draw color grid
            float x = MenuPadding;
            float y = MenuPadding;
            int col = 0;
            
            foreach (var color in colors)
            {
                Rect colorRect = new Rect(
                    inRect.x + x, 
                    inRect.y + y, 
                    ColorSquareSize, 
                    ColorSquareSize
                );
                
                bool isHovering = Mouse.IsOver(colorRect);
                
                // Draw light highlight background for all items
                GUI.color = new Color(1f, 1f, 1f, 0.04f);
                GUI.DrawTexture(colorRect.ExpandedBy(2f), BaseContent.WhiteTex);
                GUI.color = Color.white;
                
                // Draw hover highlight
                if (isHovering)
                {
                    Verse.Widgets.DrawHighlight(colorRect.ExpandedBy(2f));
                }
                
                // Draw color square
                if (color.HasValue)
                {
                    Verse.Widgets.DrawBoxSolid(colorRect, color.Value);
                }
                else
                {
                    // Draw default color option with checkerboard pattern to indicate "default"
                    Color defaultColor = Main.Instance?.GetDefaultLabelColor() ?? Color.white;
                    Verse.Widgets.DrawBoxSolid(colorRect, defaultColor);
                    
                    // Add subtle overlay to indicate this is the default option
                    GUI.color = new Color(0f, 0f, 0f, 0.2f);
                    GUI.DrawTexture(colorRect.ContractedBy(colorRect.width * 0.3f), BaseContent.WhiteTex);
                    GUI.color = Color.white;
                    
                    // Add "Default" text
                    GUI.color = GetContrastingTextColor(defaultColor);
                    Text.Font = GameFont.Tiny;
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Verse.Widgets.Label(colorRect, "D");
                    Text.Anchor = TextAnchor.UpperLeft;
                    Text.Font = GameFont.Small;
                    GUI.color = Color.white;
                }
                
                // Draw selection indicator with thicker border
                if ((color == selectedColor) || (color == null && selectedColor == null))
                {
                    Verse.Widgets.DrawBox(colorRect, 3);
                    // Add corner indicators for selected item
                    GUI.color = new Color(1f, 1f, 1f, 0.5f);
                    Verse.Widgets.DrawBox(colorRect.ContractedBy(2f), 1);
                    GUI.color = Color.white;
                }
                else
                {
                    Verse.Widgets.DrawBox(colorRect, 1);
                }
                
                // Handle hover sound and tooltip
                if (isHovering)
                {
                    MouseoverSounds.DoRegion(colorRect);
                    
                    string tooltip;
                    if (color.HasValue)
                    {
                        tooltip = $"RGB: ({color.Value.r:F2}, {color.Value.g:F2}, {color.Value.b:F2})";
                    }
                    else
                    {
                        tooltip = "FALCLF.UseDefaultColor".Translate();
                    }
                    TooltipHandler.TipRegion(colorRect, tooltip);
                }
                
                // Handle click
                if (Verse.Widgets.ButtonInvisible(colorRect))
                {
                    onColorSelected?.Invoke(color);
                    SoundDefOf.Click.PlayOneShotOnCamera();
                    Close();
                    return;
                }
                
                // Move to next grid position
                col++;
                if (col >= ColorsPerRow)
                {
                    col = 0;
                    x = MenuPadding;
                    y += ColorSquareSize + ColorSpacing;
                }
                else
                {
                    x += ColorSquareSize + ColorSpacing;
                }
            }
            
            // Close on any click outside color squares
            if (Event.current.type == EventType.MouseDown)
            {
                Event.current.Use();
                Close();
            }
        }
        
        public override void PostClose()
        {
            base.PostClose();
            // Play close sound if needed
            SoundDefOf.FloatMenu_Cancel?.PlayOneShotOnCamera();
        }
        
        /// <summary>
        /// Get contrasting text color (black or white) for given background
        /// </summary>
        private static Color GetContrastingTextColor(Color backgroundColor)
        {
            float brightness = (backgroundColor.r + backgroundColor.g + backgroundColor.b) / 3f;
            return brightness > 0.5f ? Color.black : Color.white;
        }
    }
}