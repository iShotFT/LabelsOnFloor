using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace LabelsOnFloor.SettingsLibrary.Widgets
{
    /// <summary>
    /// Static helper class for creating color dropdown buttons - part of the settings library
    /// </summary>
    public static class ColorDropdownWidget
    {
        // Standard color palette
        public static readonly List<Color?> StandardColorPalette = new List<Color?>
        {
            null, // Default/no custom color
            Color.white,
            new Color(0.9f, 0.9f, 0.9f), // Light gray
            new Color(0.7f, 0.7f, 0.7f), // Medium gray
            new Color(0.5f, 0.5f, 0.5f), // Gray
            new Color(0.3f, 0.3f, 0.3f), // Dark gray
            
            new Color(1f, 0.2f, 0.2f), // Red
            new Color(1f, 0.5f, 0.2f), // Orange
            new Color(1f, 1f, 0.2f), // Yellow
            new Color(0.2f, 1f, 0.2f), // Green
            new Color(0.2f, 1f, 1f), // Cyan
            new Color(0.2f, 0.2f, 1f), // Blue
            new Color(0.8f, 0.2f, 1f), // Purple
            new Color(1f, 0.2f, 0.8f), // Pink
            
            new Color(0.6f, 0.2f, 0.2f), // Dark red
            new Color(0.6f, 0.3f, 0.1f), // Brown
            new Color(0.6f, 0.6f, 0.2f), // Dark yellow
            new Color(0.1f, 0.6f, 0.1f), // Dark green
            new Color(0.1f, 0.6f, 0.6f), // Dark cyan
            new Color(0.1f, 0.1f, 0.6f), // Dark blue
            new Color(0.4f, 0.1f, 0.6f), // Dark purple
            new Color(0.6f, 0.1f, 0.4f), // Dark pink
        };
        
        /// <summary>
        /// Draws a color dropdown button and handles opening the dropdown
        /// </summary>
        /// <param name="rect">Rectangle for the button</param>
        /// <param name="currentColor">Currently selected color</param>
        /// <param name="defaultColor">Default color when null is selected</param>
        /// <param name="onColorSelected">Callback when color is selected</param>
        /// <returns>True if color was changed</returns>
        public static bool DrawColorDropdownButton(Rect rect, ref Color? currentColor, Color defaultColor, Action<Color?> onColorSelected = null)
        {
            Color displayColor = currentColor ?? defaultColor;
            bool colorChanged = false;
            
            // Draw RimWorld-style button frame
            Verse.Widgets.DrawAtlas(rect, TexUI.FloatMenuOptionBG);
            
            // Create sections: color preview on left, text in middle, dropdown arrow on right
            float colorPreviewSize = rect.height - 6f;
            Rect colorPreviewRect = new Rect(rect.x + 3f, rect.y + 3f, colorPreviewSize, colorPreviewSize);
            Rect labelRect = new Rect(colorPreviewRect.xMax + 6f, rect.y, rect.width - colorPreviewSize - 40f, rect.height);
            Rect arrowRect = new Rect(rect.xMax - 25f, rect.y, 20f, rect.height);
            
            // Highlight on hover
            if (Mouse.IsOver(rect))
            {
                Verse.Widgets.DrawHighlight(rect);
            }
            
            // Draw color preview with border
            Verse.Widgets.DrawBoxSolid(colorPreviewRect, displayColor);
            Verse.Widgets.DrawBox(colorPreviewRect, 2);
            
            // Draw label
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = Color.white;
            string label;
            if (currentColor.HasValue)
            {
                // Show RGB values for custom color
                Color c = currentColor.Value;
                label = $"RGB ({(int)(c.r * 255)}, {(int)(c.g * 255)}, {(int)(c.b * 255)})";
            }
            else
            {
                label = "FALCLF.DefaultColorLabel".Translate();
            }
            Verse.Widgets.Label(labelRect, label);
            
            // Draw dropdown arrow
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;
            Verse.Widgets.Label(arrowRect, "▼");
            
            // Reset text settings
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            
            // Handle button click
            if (Verse.Widgets.ButtonInvisible(rect))
            {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                Find.WindowStack.Add(new ColorGridFloatMenu(
                    StandardColorPalette,
                    currentColor,
                    onColorSelected,
                    defaultColor,
                    "FALCLF.UseDefaultColor"
                ));
            }
            
            return colorChanged;
        }
        
        /// <summary>
        /// Draws a minimal color dropdown button without text label (for settings page)
        /// </summary>
        /// <param name="rect">Rectangle for the button</param>
        /// <param name="currentColor">Currently selected color</param>
        /// <param name="defaultColor">Default color when null is selected</param>
        /// <param name="onColorSelected">Callback when color is selected</param>
        /// <param name="tooltip">Optional tooltip text</param>
        /// <param name="defaultColorLabelKey">Translation key for default color label</param>
        /// <returns>True if color was changed</returns>
        public static bool DrawMinimalColorDropdownButton(
            Rect rect, 
            ref Color? currentColor, 
            Color defaultColor, 
            Action<Color?> onColorSelected = null, 
            string tooltip = null,
            string defaultColorLabelKey = null)
        {
            Color displayColor = currentColor ?? defaultColor;
            bool colorChanged = false;
            
            // Draw RimWorld-style button frame
            Verse.Widgets.DrawAtlas(rect, TexUI.FloatMenuOptionBG);
            
            // Highlight on hover
            if (Mouse.IsOver(rect))
            {
                Verse.Widgets.DrawHighlight(rect);
            }
            
            // Draw color preview filling most of the button
            float margin = 4f;
            Rect colorRect = rect.ContractedBy(margin);
            
            // Leave space for dropdown arrow
            colorRect.width -= 20f;
            Verse.Widgets.DrawBoxSolid(colorRect, displayColor);
            Verse.Widgets.DrawBox(colorRect, 2);
            
            // Draw dropdown arrow
            Rect arrowRect = new Rect(rect.xMax - 20f, rect.y, 20f, rect.height);
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;
            GUI.color = GetContrastingTextColor(displayColor);
            Verse.Widgets.Label(arrowRect, "▼");
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            
            // Add tooltip if provided
            if (!string.IsNullOrEmpty(tooltip))
            {
                TooltipHandler.TipRegion(rect, tooltip);
            }
            
            // Handle button click
            if (Verse.Widgets.ButtonInvisible(rect))
            {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                Find.WindowStack.Add(new ColorGridFloatMenu(
                    StandardColorPalette,
                    currentColor,
                    onColorSelected,
                    defaultColor,
                    defaultColorLabelKey
                ));
            }
            
            return colorChanged;
        }
        
        /// <summary>
        /// Get contrasting text color (black or white) for given background
        /// </summary>
        public static Color GetContrastingTextColor(Color backgroundColor)
        {
            float brightness = (backgroundColor.r + backgroundColor.g + backgroundColor.b) / 3f;
            return brightness > 0.5f ? Color.black : Color.white;
        }
    }
    
    /// <summary>
    /// Custom FloatMenu that displays a grid of color squares for selection
    /// Part of the settings library
    /// </summary>
    public class ColorGridFloatMenu : Window
    {
        private List<Color?> colors;
        private Color? selectedColor;
        private Action<Color?> onColorSelected;
        private Color defaultColor;
        private string defaultColorLabelKey;
        
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
        
        public ColorGridFloatMenu(
            List<Color?> colorOptions, 
            Color? currentSelection, 
            Action<Color?> onSelection,
            Color defaultColor,
            string defaultColorLabelKey = null)
        {
            colors = colorOptions;
            selectedColor = currentSelection;
            onColorSelected = onSelection;
            this.defaultColor = defaultColor;
            this.defaultColorLabelKey = defaultColorLabelKey ?? "FALCLF.UseDefaultColor";
            
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
                    // Draw default color option
                    Verse.Widgets.DrawBoxSolid(colorRect, defaultColor);
                    
                    // Add subtle overlay to indicate this is the default option
                    GUI.color = new Color(0f, 0f, 0f, 0.2f);
                    GUI.DrawTexture(colorRect.ContractedBy(colorRect.width * 0.3f), BaseContent.WhiteTex);
                    GUI.color = Color.white;
                    
                    // Add "Default" text
                    GUI.color = ColorDropdownWidget.GetContrastingTextColor(defaultColor);
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
                        tooltip = defaultColorLabelKey.Translate();
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
    }
}