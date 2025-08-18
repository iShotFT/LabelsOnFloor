using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace LabelsOnFloor.SettingsLibrary.Core
{
    /// <summary>
    /// Main framework for creating professional mod settings pages with proper two-column layout
    /// This is a standalone library that can be used in any RimWorld mod
    /// </summary>
    public class ModSettingsFramework
    {
        private readonly List<ISettingsCategory> categories = new List<ISettingsCategory>();
        private readonly ModSettingsConfiguration config;
        private Vector2 scrollPosition = Vector2.zero;
        private float totalHeight = 0f;
        private Rect lastInRect;
        private readonly string modName;
        
        // Performance optimization: Framework-level dirty tracking
        private bool frameworkDirty = true;
        private int lastDrawFrame = -1;
        private float cachedTotalHeight = 0f;
        private float lastWindowWidth = 0f;
        
        // Default configuration values
        public static class Defaults
        {
            public const float RowHeight = 30f;
            public const float CategoryHeaderHeight = 32f;
            public const float CategoryGap = 16f;
            public const float ItemGap = 2f;
            public const float IndentWidth = 24f;
            public const float ScrollBarWidth = 16f;
            public const float SideMargin = 8f;
            public const float LabelColumnRatio = 0.45f;  // 45% for labels
            public const float ControlPadding = 10f;
            public const float MinLabelWidth = 200f;
            public const float MinControlWidth = 180f;
            public const float TitleHeight = 40f;  // Space reserved for mod title
        }
        
        public ModSettingsFramework(string modName, ModSettingsConfiguration config = null)
        {
            this.modName = modName;
            this.config = config ?? ModSettingsConfiguration.Default;
        }
        
        /// <summary>
        /// Add a category to the settings page
        /// </summary>
        public ISettingsCategory AddCategory(string labelKey, string descriptionKey = null)
        {
            var category = new SettingsCategory(labelKey, descriptionKey, config);
            categories.Add(category);
            return category;
        }
        
        /// <summary>
        /// Main rendering method - call this from your mod's DoSettingsWindowContents
        /// </summary>
        public void DoSettingsWindowContents(Rect inRect)
        {
            // Store the rect for proper bounds checking
            lastInRect = inRect;
            
            // IMPORTANT: Dialog_ModSettings already draws the title ABOVE the inRect we receive
            // The inRect we get starts at y=40 (after the title) and has height reduced by 40 + CloseButSize.y
            // However, there seems to be rendering overlap issues, so we add extra top padding
            
            // Add extra top padding to avoid title overlap
            float topPadding = 10f;
            inRect.y += topPadding;
            inRect.height -= topPadding;
            
            // The Close button is already handled by Dialog_ModSettings outside our rect
            // We get the full content area to use
            float contentHeight = inRect.height;
            
            // Performance optimization: Only recalculate height when window resized
            bool windowResized = Math.Abs(inRect.width - lastWindowWidth) > 0.1f;
            if (windowResized || cachedTotalHeight == 0f)
            {
                lastWindowWidth = inRect.width;
                CalculateTotalHeight(inRect.width);
                cachedTotalHeight = totalHeight;
                frameworkDirty = true;
            }
            else
            {
                totalHeight = cachedTotalHeight;
            }
            bool needsScrollBar = totalHeight > contentHeight;
            
            // Adjust for scrollbar
            float availableWidth = inRect.width - (config.SideMargin * 2f);
            if (needsScrollBar)
            {
                availableWidth -= Defaults.ScrollBarWidth;
            }
            
            // Calculate column widths
            float labelWidth = Mathf.Max(config.MinLabelWidth, availableWidth * config.LabelColumnRatio);
            float controlWidth = availableWidth - labelWidth - config.ControlPadding;
            
            // Ensure minimum control width
            if (controlWidth < config.MinControlWidth)
            {
                // Adjust label width to ensure minimum control width
                controlWidth = config.MinControlWidth;
                labelWidth = availableWidth - controlWidth - config.ControlPadding;
            }
            
            // Define scroll area
            Rect scrollRect = new Rect(0f, 0f, inRect.width, contentHeight);
            Rect viewRect = new Rect(0f, 0f, availableWidth + config.SideMargin, totalHeight);
            
            // Begin scroll view
            Verse.Widgets.BeginScrollView(scrollRect, ref scrollPosition, viewRect);
            
            float curY = config.SideMargin;
            
            // Draw each category
            foreach (var category in categories)
            {
                if (!category.IsVisible()) continue;
                
                curY = DrawCategory(config.SideMargin, curY, labelWidth, controlWidth, category);
                curY += config.CategoryGap;
            }
            
            Verse.Widgets.EndScrollView();
            
            // Draw reset button if enabled
            if (config.ShowResetButton)
            {
                DrawResetButton(inRect);
            }
        }
        
        private float DrawCategory(float x, float y, float labelWidth, float controlWidth, ISettingsCategory category)
        {
            float startY = y;
            
            // Draw category header
            Text.Font = GameFont.Medium;
            GUI.color = new Color(1f, 1f, 1f, 0.9f);
            Rect headerRect = new Rect(x, y, labelWidth + controlWidth + config.ControlPadding, config.CategoryHeaderHeight);
            Verse.Widgets.Label(headerRect, category.GetLabel().Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            y += config.CategoryHeaderHeight;
            
            // Draw category description if present
            string description = category.GetDescription();
            if (!string.IsNullOrEmpty(description))
            {
                Text.Font = GameFont.Tiny;
                GUI.color = new Color(0.8f, 0.8f, 0.8f);
                float descHeight = Text.CalcHeight(description.Translate(), labelWidth + controlWidth);
                Rect descRect = new Rect(x, y, labelWidth + controlWidth + config.ControlPadding, descHeight);
                Verse.Widgets.Label(descRect, description.Translate());
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
                y += descHeight + 4f;
            }
            
            // Draw separator line
            Color lineColor = new Color(1f, 1f, 1f, 0.2f);
            Verse.Widgets.DrawLineHorizontal(x, y, labelWidth + controlWidth + config.ControlPadding, lineColor);
            y += 6f;
            
            // Draw items
            foreach (var item in category.GetItems())
            {
                if (!item.IsVisible()) continue;
                
                // Check if item should be disabled
                bool wasEnabled = GUI.enabled;
                if (!item.IsEnabled())
                {
                    GUI.enabled = false;
                }
                
                // Calculate indent
                float indent = item.GetIndentLevel() * config.IndentWidth;
                
                // Draw the item
                float itemHeight = item.Draw(
                    x + indent, 
                    y, 
                    labelWidth - indent, 
                    controlWidth,
                    config
                );
                
                y += itemHeight + config.ItemGap;
                
                GUI.enabled = wasEnabled;
            }
            
            return y;
        }
        
        private void DrawResetButton(Rect inRect)
        {
            // Draw reset as a small text link in the top right corner
            Text.Font = GameFont.Tiny;
            string resetText = config.ResetButtonLabelKey.Translate();
            float textWidth = Text.CalcSize(resetText).x + 10f;
            
            Rect resetRect = new Rect(
                inRect.width - textWidth - 5f,
                5f,
                textWidth,
                20f
            );
            
            // Draw as a subtle link
            GUI.color = Mouse.IsOver(resetRect) ? new Color(0.9f, 0.9f, 1f) : new Color(0.7f, 0.7f, 0.7f);
            if (Verse.Widgets.ButtonText(resetRect, resetText, drawBackground: false))
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    config.ResetConfirmMessageKey.Translate(),
                    () => {
                        config.ResetToDefaults?.Invoke();
                        // Force refresh  
                        scrollPosition = Vector2.zero;
                    },
                    destructive: true
                ));
            }
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }
        
        private void CalculateTotalHeight(float width)
        {
            totalHeight = config.SideMargin;
            
            foreach (var category in categories)
            {
                if (!category.IsVisible()) continue;
                
                // Category header
                totalHeight += config.CategoryHeaderHeight;
                
                // Category description
                string desc = category.GetDescription();
                if (!string.IsNullOrEmpty(desc))
                {
                    float descHeight = Text.CalcHeight(desc.Translate(), width - (config.SideMargin * 2f));
                    totalHeight += descHeight + 4f;
                }
                
                // Separator
                totalHeight += 6f;
                
                // Items
                foreach (var item in category.GetItems())
                {
                    if (!item.IsVisible()) continue;
                    totalHeight += item.GetHeight() + config.ItemGap;
                }
                
                totalHeight += config.CategoryGap;
            }
            
            totalHeight += config.SideMargin;
        }
        
        /// <summary>
        /// Mark the framework as needing layout recalculation
        /// </summary>
        public void MarkDirty()
        {
            frameworkDirty = true;
            cachedTotalHeight = 0f;
        }
    }
    
    /// <summary>
    /// Configuration for the settings framework
    /// </summary>
    public class ModSettingsConfiguration
    {
        public float RowHeight { get; set; } = ModSettingsFramework.Defaults.RowHeight;
        public float CategoryHeaderHeight { get; set; } = ModSettingsFramework.Defaults.CategoryHeaderHeight;
        public float CategoryGap { get; set; } = ModSettingsFramework.Defaults.CategoryGap;
        public float ItemGap { get; set; } = ModSettingsFramework.Defaults.ItemGap;
        public float IndentWidth { get; set; } = ModSettingsFramework.Defaults.IndentWidth;
        public float SideMargin { get; set; } = ModSettingsFramework.Defaults.SideMargin;
        public float LabelColumnRatio { get; set; } = ModSettingsFramework.Defaults.LabelColumnRatio;
        public float ControlPadding { get; set; } = ModSettingsFramework.Defaults.ControlPadding;
        public float MinLabelWidth { get; set; } = ModSettingsFramework.Defaults.MinLabelWidth;
        public float MinControlWidth { get; set; } = ModSettingsFramework.Defaults.MinControlWidth;
        public bool ShowResetButton { get; set; } = true;
        public Action ResetToDefaults { get; set; }
        
        // Translation keys for reset functionality
        public string ResetButtonLabelKey { get; set; } = "FALCLF.ResetToDefaults";
        public string ResetConfirmMessageKey { get; set; } = "FALCLF.ConfirmResetSettings";
        
        // Translation keys for color picker
        public string DefaultColorLabelKey { get; set; } = "FALCLF.DefaultColor";
        public string UseDefaultColorKey { get; set; } = "FALCLF.UseDefaultColor";
        
        public static ModSettingsConfiguration Default => new ModSettingsConfiguration();
    }
}