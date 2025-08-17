using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using LabelsOnFloor.FontLibrary;
using RimWorld;
using Verse.Sound;

namespace LabelsOnFloor.SettingsLibrary.Widgets
{
    public static class FontDropdownWidget
    {
        
        public static bool ButtonDropdown(Rect rect, string currentFontName, Action<string> onFontSelected)
        {
            var font = FontRegistry.GetFont(currentFontName);
            
            // Draw RimWorld-style button frame (matching other dropdowns)
            Verse.Widgets.DrawAtlas(rect, TexUI.FloatMenuOptionBG);
            
            // Highlight on hover
            if (Mouse.IsOver(rect))
            {
                Verse.Widgets.DrawHighlight(rect);
            }
            
            // Draw the preview image in the button (no text, just the preview)
            if (font?.PreviewTexture != null)
            {
                // Calculate preview position (left-aligned with padding, vertically centered)
                float maxPreviewWidth = rect.width - 30f; // Leave space for dropdown arrow
                float previewHeight = Mathf.Min(font.PreviewTexture.height, rect.height - 4f);
                float aspectRatio = (float)font.PreviewTexture.width / font.PreviewTexture.height;
                float previewWidth = Mathf.Min(previewHeight * aspectRatio, maxPreviewWidth);
                
                Rect previewRect = new Rect(
                    rect.x + 8f,
                    rect.y + (rect.height - previewHeight) / 2f,
                    previewWidth,
                    previewHeight
                );
                
                GUI.color = Color.white;
                GUI.DrawTexture(previewRect, font.PreviewTexture, ScaleMode.ScaleToFit);
            }
            else
            {
                // Fallback to text if no preview available
                var buttonLabel = font?.Name ?? currentFontName;
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = Color.white;
                var textRect = new Rect(rect.x + 8f, rect.y, rect.width - 30f, rect.height);
                Verse.Widgets.Label(textRect, buttonLabel);
            }
            
            // Draw dropdown arrow on the right side (consistent with other dropdowns)
            var arrowRect = new Rect(rect.xMax - 20f, rect.y, 20f, rect.height);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Verse.Widgets.Label(arrowRect, "▼");
            
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            
            // Handle button click
            if (Verse.Widgets.ButtonInvisible(rect))
            {
                SoundDefOf.Tick_Tiny.PlayOneShotOnCamera();
                ShowFontDropdownMenu(rect, currentFontName, onFontSelected);
                return true;
            }
            
            return false;
        }
        
        
        private static void ShowFontDropdownMenu(Rect buttonRect, string currentFontName, Action<string> onFontSelected)
        {
            var fontNames = FontRegistry.GetRegisteredFontNames().ToList();
            
            // Use custom font preview window instead of standard FloatMenu
            Find.WindowStack.Add(new FontPreviewFloatMenu(
                fontNames,
                currentFontName,
                onFontSelected
            ));
        }
    }
    
    /// <summary>
    /// Custom FloatMenu that displays font options with preview rendering
    /// </summary>
    public class FontPreviewFloatMenu : Window
    {
        private List<string> fontNames;
        private string selectedFont;
        private Action<string> onFontSelected;
        
        // Visual settings
        private const float MenuWidth = 250f;
        private const float ItemHeight = 38f;  // Reduced but still room for indicators
        private const float MenuPadding = 3f;
        private static readonly Color ColorBGActive = new ColorInt(21, 25, 29).ToColor;
        private static readonly Color ColorBGHover = new ColorInt(29, 45, 50).ToColor;
        private static readonly Vector2 InitialPositionShift = new Vector2(4f, 0f);
        
        private float menuHeight;
        
        public override Vector2 InitialSize => new Vector2(MenuWidth, menuHeight);
        
        protected override float Margin => 0f;
        
        public FontPreviewFloatMenu(
            List<string> fonts,
            string currentSelection,
            Action<string> onSelection)
        {
            fontNames = fonts;
            selectedFont = currentSelection;
            onFontSelected = onSelection;
            
            // Calculate menu height
            menuHeight = (fonts.Count * ItemHeight) + (MenuPadding * 2);
            
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
            
            // Draw font options
            float y = MenuPadding;
            
            foreach (var fontName in fontNames)
            {
                var font = FontRegistry.GetFont(fontName);
                if (font == null) continue;
                
                Rect itemRect = new Rect(
                    inRect.x + MenuPadding,
                    inRect.y + y,
                    MenuWidth - (MenuPadding * 2),
                    ItemHeight
                );
                
                bool isHovering = Mouse.IsOver(itemRect);
                bool isSelected = fontName == selectedFont;
                
                // Draw hover highlight
                if (isHovering)
                {
                    GUI.color = ColorBGHover;
                    GUI.DrawTexture(itemRect, BaseContent.WhiteTex);
                    GUI.color = Color.white;
                    Verse.Widgets.DrawHighlight(itemRect);
                }
                
                // Draw selection indicator
                if (isSelected)
                {
                    Verse.Widgets.DrawBoxSolid(itemRect.LeftPartPixels(3f), new Color(0.5f, 0.7f, 1f));
                }
                
                // Draw font name with preview
                // We'll render the font name using the actual font material
                DrawFontPreview(itemRect, fontName, font);
                
                // Handle hover sound
                if (isHovering)
                {
                    MouseoverSounds.DoRegion(itemRect);
                }
                
                // Handle click
                if (Verse.Widgets.ButtonInvisible(itemRect))
                {
                    onFontSelected?.Invoke(fontName);
                    SoundDefOf.Click.PlayOneShotOnCamera();
                    Close();
                    return;
                }
                
                y += ItemHeight;
            }
            
            // Close on any click outside items
            if (Event.current.type == EventType.MouseDown)
            {
                Event.current.Use();
                Close();
            }
        }
        
        private void DrawFontPreview(Rect rect, string fontName, IFont font)
        {
            // Use the pre-rendered Preview.png image
            if (font?.PreviewTexture != null)
            {
                // Reserve space for language indicators below
                float previewAreaHeight = rect.height * 0.6f;  // Top 60% for preview
                float indicatorAreaHeight = rect.height * 0.4f;  // Bottom 40% for indicators
                
                // Calculate preview dimensions
                float previewHeight = Mathf.Min(font.PreviewTexture.height, previewAreaHeight - 2f);
                float aspectRatio = (float)font.PreviewTexture.width / font.PreviewTexture.height;
                float previewWidth = previewHeight * aspectRatio;
                
                // Ensure it doesn't overflow horizontally
                float maxPreviewWidth = rect.width - 20f;
                if (previewWidth > maxPreviewWidth)
                {
                    previewWidth = maxPreviewWidth;
                    previewHeight = previewWidth / aspectRatio;
                }
                
                // Draw preview in top portion, left-aligned
                Rect previewRect = new Rect(
                    rect.x + 12f,
                    rect.y + 2f,  // Small top padding
                    previewWidth,
                    previewHeight
                );
                
                GUI.color = Color.white;
                GUI.DrawTexture(previewRect, font.PreviewTexture, ScaleMode.ScaleToFit);
                
                // Draw language support indicators below the preview
                Rect indicatorRect = new Rect(
                    rect.x,
                    rect.y + previewAreaHeight,
                    rect.width,
                    indicatorAreaHeight
                );
                DrawLanguageSupport(indicatorRect, font);
            }
            else
            {
                // Fallback to text if no preview available
                Text.Font = GameFont.Small;
                Text.Anchor = TextAnchor.MiddleLeft;
                GUI.color = Color.white;
                
                float textAreaHeight = rect.height * 0.6f;
                Rect textRect = new Rect(rect.x + 12f, rect.y, rect.width - 20f, textAreaHeight);
                Verse.Widgets.Label(textRect, fontName);
                
                // Draw language support indicators below
                Rect indicatorRect = new Rect(
                    rect.x,
                    rect.y + textAreaHeight,
                    rect.width,
                    rect.height * 0.4f
                );
                DrawLanguageSupport(indicatorRect, font);
            }
            
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }
        
        private void DrawLanguageSupport(Rect rect, IFont font)
        {
            if (font == null) return;
            
            // Get language support from font
            var genericFont = font as GenericFont;
            if (genericFont == null) return;
            
            var metadata = genericFont.GetMetadata();
            if (metadata == null) return;
            
            // Draw language support indicators with boxes
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            
            // Define colors
            Color mutedGreen = new Color(0.4f, 0.7f, 0.4f, 0.8f);  // Muted green for supported
            Color mutedRed = new Color(0.7f, 0.4f, 0.4f, 0.8f);    // Muted red for unsupported
            Color borderColor = new Color(0.8f, 0.8f, 0.8f, 0.5f);  // Muted white border
            
            // Draw indicators left to right with boxes
            string[] indicators = { "Lat", "Ext", "Cyr" };
            bool[] supported = { 
                metadata.hasLatinSupport,
                metadata.hasAccentSupport,
                metadata.hasCyrillicSupport
            };
            
            float xPos = rect.x + 12f;  // Same left padding as preview
            float boxWidth = 24f;
            float boxHeight = rect.height - 2f;
            float spacing = 2f;
            
            for (int i = 0; i < indicators.Length; i++)
            {
                Rect boxRect = new Rect(
                    xPos,
                    rect.y + 1f,
                    boxWidth,
                    boxHeight
                );
                
                // Draw border
                GUI.color = borderColor;
                Verse.Widgets.DrawBox(boxRect, 1);
                
                // Draw text with appropriate color
                GUI.color = supported[i] ? mutedGreen : mutedRed;
                Verse.Widgets.Label(boxRect, indicators[i]);
                
                xPos += boxWidth + spacing;
            }
            
            // Reset
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Small;
        }
        
        
        public override void PostClose()
        {
            base.PostClose();
            // Play close sound if needed
            SoundDefOf.FloatMenu_Cancel?.PlayOneShotOnCamera();
        }
    }
}