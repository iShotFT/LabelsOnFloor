using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using LabelsOnFloor.SettingsLibrary.Widgets;

namespace LabelsOnFloor
{
    public class Dialog_RenameZoneWithColor : Window
    {
        private readonly Zone _zone;
        private readonly CustomZoneData _customZoneData;
        
        private string curName;
        private string originalName;
        private Color? selectedColor;
        private bool? showLabel;  // null = use global, true = always show, false = always hide
        private Vector2? positionOffset;  // null = no offset, X/Z offset in cells
        private bool focusedNameField;
        private int startAcceptingInputAtFrame;
        private bool hasSelectedText;
        
        // Store original values for cancel
        private string originalCustomLabel;
        private Color? originalCustomColor;
        private bool? originalShowLabel;
        private Vector2? originalPositionOffset;
        
        // Standard RimWorld dialog sizing
        private const float LabelWidth = 100f;
        private const float FieldSpacing = 10f;
        private const float RowHeight = 30f;
        private const float ButtonHeight = 30f;
        private const float ArrowButtonSize = 24f;
        
        public override Vector2 InitialSize => new Vector2(380f, 300f);  // Compact with 2-row position controls
        
        public Dialog_RenameZoneWithColor(Zone zone) : this(zone, GetOrCreateCustomZoneData(zone))
        {
        }
        
        public Dialog_RenameZoneWithColor(Zone zone, CustomZoneData customZoneData)
        {
            _zone = zone;
            _customZoneData = customZoneData;
            
            // Get what would be displayed on the floor label
            originalName = GetZoneDisplayName(zone);
            
            // The input box should show what's actually rendered on the floor:
            // - If there's a custom label, show that
            // - Otherwise, for growing zones, show the plant name
            // - Otherwise, show the zone's label
            if (!string.IsNullOrEmpty(customZoneData.Label))
            {
                curName = customZoneData.Label.ToUpper();
            }
            else if (zone is Zone_Growing growingZone)
            {
                var plantDef = growingZone.GetPlantDefToGrow();
                curName = plantDef?.label?.ToUpper() ?? zone.label?.ToUpper() ?? "";
            }
            else
            {
                curName = zone.label?.ToUpper() ?? "";
            }
            selectedColor = customZoneData.CustomColor;
            showLabel = customZoneData.ShowLabel;
            positionOffset = customZoneData.PositionOffset;
            
            // Store original values for cancel
            originalCustomLabel = customZoneData.Label;
            originalCustomColor = customZoneData.CustomColor;
            originalShowLabel = customZoneData.ShowLabel;
            originalPositionOffset = customZoneData.PositionOffset;
            
            forcePause = true;
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            
            // Accept input immediately by default (unless opened by hotkey)
            startAcceptingInputAtFrame = 0;
        }
        
        public void WasOpenedByHotkey()
        {
            // Skip input for 1 frame when opened by hotkey to prevent the key from appearing in the text field
            startAcceptingInputAtFrame = Time.frameCount + 1;
        }
        
        private bool AcceptsInput => startAcceptingInputAtFrame <= Time.frameCount;
        
        public override void DoWindowContents(Rect inRect)
        {
            // Title at the top
            Text.Font = GameFont.Medium;
            Rect titleRect = new Rect(0f, 0f, inRect.width, Text.LineHeight);
            string title = "FALCLF.RenameZone".Translate();
            Verse.Widgets.Label(titleRect, title);
            Text.Font = GameFont.Small;
            
            float curY = titleRect.yMax + 15f;
            
            // Calculate field width based on available space
            float fieldWidth = inRect.width - LabelWidth - FieldSpacing;
            
            // Two-column layout for fields
            // Row 1: Zone Name
            Rect nameLabelRect = new Rect(0f, curY, LabelWidth, RowHeight);
            Text.Anchor = TextAnchor.MiddleLeft;
            Verse.Widgets.Label(nameLabelRect, "FALCLF.ZoneName".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            
            Rect nameFieldRect = new Rect(LabelWidth + FieldSpacing, curY, fieldWidth, RowHeight);
            GUI.SetNextControlName("RenameField");
            
            // Handle text field with proper selection for hotkey opening
            if (AcceptsInput)
            {
                // Use Verse.Widgets.TextField for consistency with room dialog
                string tempName = Verse.Widgets.TextField(nameFieldRect, curName);
                // Force uppercase and limit length
                tempName = tempName.ToUpper();
                if (tempName.Length > 28)
                {
                    tempName = tempName.Substring(0, 28);
                }
                curName = tempName;
                
                // Select all text when opened by hotkey (on the first frame we accept input)
                if (!focusedNameField)
                {
                    GUI.FocusControl("RenameField");
                    focusedNameField = true;
                    
                    // If opened by hotkey, select all text
                    if (startAcceptingInputAtFrame > 0 && !hasSelectedText)
                    {
                        TextEditor textEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
                        if (textEditor != null)
                        {
                            textEditor.SelectAll();
                            hasSelectedText = true;
                        }
                    }
                }
            }
            else
            {
                // Just display the field without allowing input yet
                Verse.Widgets.TextField(nameFieldRect, curName);
            }
            
            curY += RowHeight + 5f;
            
            // Row 2: Label Color
            Rect colorLabelRect = new Rect(0f, curY, LabelWidth, RowHeight);
            Text.Anchor = TextAnchor.MiddleLeft;
            Verse.Widgets.Label(colorLabelRect, "FALCLF.LabelColor".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            
            // Color selector button
            Rect colorButtonRect = new Rect(LabelWidth + FieldSpacing, curY, fieldWidth, RowHeight);
            ColorDropdownWidget.DrawColorDropdownButton(
                colorButtonRect, 
                ref selectedColor,
                Main.Instance.GetDefaultLabelColor(),
                (color) => {
                    selectedColor = color;
                }
            );
            
            curY += RowHeight + 5f;
            
            // Row 3: Visibility Override
            Rect visibilityLabelRect = new Rect(0f, curY, LabelWidth, RowHeight);
            Text.Anchor = TextAnchor.MiddleLeft;
            Verse.Widgets.Label(visibilityLabelRect, "FALCLF.RoomVisibility".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            
            Rect visibilityFieldRect = new Rect(LabelWidth + FieldSpacing, curY, fieldWidth, RowHeight);
            DrawVisibilityDropdown(visibilityFieldRect);
            
            curY += RowHeight + 10f;
            
            // Row 4: Position Adjustment
            Rect positionLabelRect = new Rect(0f, curY, LabelWidth, RowHeight);
            Text.Anchor = TextAnchor.MiddleLeft;
            Verse.Widgets.Label(positionLabelRect, "FALCLF.PositionAdjust".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            
            // Position controls need 2 rows with bordered style (compact: 22px buttons + 3px spacing + 10px padding)
            float positionControlHeight = 22f + 3f + 22f + 10f; // button1 + spacing + button2 + padding
            Rect positionFieldRect = new Rect(LabelWidth + FieldSpacing, curY, fieldWidth, positionControlHeight);
            DrawPositionControls(positionFieldRect);
            
            curY += positionControlHeight + 10f;
            
            // Bottom buttons (Cancel and OK)
            float buttonWidth = (inRect.width - 10f) / 2f;
            Rect cancelButtonRect = new Rect(0f, inRect.height - ButtonHeight, buttonWidth, ButtonHeight);
            Rect okButtonRect = new Rect(inRect.width - buttonWidth, inRect.height - ButtonHeight, buttonWidth, ButtonHeight);
            
            if (Verse.Widgets.ButtonText(cancelButtonRect, "Cancel".Translate()))
            {
                // Revert to original values
                _customZoneData.Label = originalCustomLabel;
                _customZoneData.CustomColor = originalCustomColor;
                _customZoneData.ShowLabel = originalShowLabel;
                _customZoneData.PositionOffset = originalPositionOffset;
                Main.Instance?.LabelPlacementHandler?.SetDirty();
                Close();
            }
            
            if (Verse.Widgets.ButtonText(okButtonRect, "OK".Translate()) || (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return))
            {
                ApplyChanges();
                Close();
            }
        }
        
        private void ApplyChanges()
        {
            // Trim whitespace from the name
            string trimmedName = curName?.Trim() ?? "";
            
            // Update zone's actual name in RimWorld
            _zone.label = trimmedName;
            
            // Update our custom data
            _customZoneData.Label = trimmedName;
            _customZoneData.CustomColor = selectedColor;
            _customZoneData.ShowLabel = showLabel;
            _customZoneData.PositionOffset = positionOffset;
            
            // Trigger label regeneration
            Main.Instance?.LabelPlacementHandler?.SetDirty();
        }
        
        private string GetZoneDisplayName(Zone zone)
        {
            if (zone == null)
                return "";
            
            // For growing zones, show the plant name in the dialog title
            if (zone is Zone_Growing growingZone)
            {
                var plantDef = growingZone.GetPlantDefToGrow();
                if (plantDef != null)
                {
                    return plantDef.label?.ToUpper() ?? growingZone.label?.ToUpper() ?? "";
                }
            }
            
            // For other zones, show their label
            return zone.label?.ToUpper() ?? "";
        }
        
        private static CustomZoneData GetOrCreateCustomZoneData(Zone zone)
        {
            if (zone?.Map == null)
                return new CustomZoneData { ZoneId = zone?.ID ?? -1 };
                
            // Get the WorldComponent (zones are saved per world, not per map)
            var world = Find.World;
            if (world == null)
                return new CustomZoneData { ZoneId = zone.ID };
                
            var manager = world.GetComponent<CustomZoneLabelManagerComponent>();
            if (manager == null)
            {
                // This shouldn't happen as it's added on world generation
                Log.Error("[LabelsOnFloor] CustomZoneLabelManagerComponent not found on world");
                return new CustomZoneData { ZoneId = zone.ID };
            }
            
            return manager.GetOrCreateCustomData(zone);
        }
        
        private void DrawVisibilityDropdown(Rect rect)
        {
            // Draw button background
            Verse.Widgets.DrawAtlas(rect, TexUI.FloatMenuOptionBG);
            
            if (Mouse.IsOver(rect))
            {
                Verse.Widgets.DrawHighlight(rect);
            }
            
            // Determine visual state and colors
            Color iconColor;
            string iconText;
            string tooltipText;
            
            if (!showLabel.HasValue)
            {
                // Default state - gray/muted
                iconColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
                iconText = "—"; // Em dash for "default"
                tooltipText = "FALCLF.RoomVisibilityDefault".Translate() + " (" + "FALCLF.RoomVisibilityDefaultDesc".Translate() + ")";
            }
            else if (showLabel.Value)
            {
                // Always show - green checkmark
                iconColor = new Color(0.2f, 0.8f, 0.2f, 1f);
                iconText = "✓"; // Checkmark
                tooltipText = "FALCLF.RoomVisibilityShow".Translate() + " (" + "FALCLF.RoomVisibilityShowDesc".Translate() + ")";
            }
            else
            {
                // Always hide - red X
                iconColor = new Color(0.8f, 0.2f, 0.2f, 1f);
                iconText = "✕"; // X mark
                tooltipText = "FALCLF.RoomVisibilityHide".Translate() + " (" + "FALCLF.RoomVisibilityHideDesc".Translate() + ")";
            }
            
            // Draw icon
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = iconColor;
            Rect iconRect = new Rect(rect.x, rect.y, rect.height, rect.height);
            Verse.Widgets.Label(iconRect, iconText);
            
            // Draw text label
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect labelRect = new Rect(rect.x + rect.height + 4f, rect.y, rect.width - rect.height - 8f, rect.height);
            string labelText = !showLabel.HasValue ? "FALCLF.RoomVisibilityDefault".Translate() :
                              showLabel.Value ? "FALCLF.RoomVisibilityShow".Translate() :
                              "FALCLF.RoomVisibilityHide".Translate();
            Verse.Widgets.Label(labelRect, labelText);
            
            // Reset text settings
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            
            // Add tooltip
            TooltipHandler.TipRegion(rect, tooltipText);
            
            // Handle click to cycle through states
            if (Verse.Widgets.ButtonInvisible(rect))
            {
                // Cycle: null -> true -> false -> null
                if (!showLabel.HasValue)
                    showLabel = true;
                else if (showLabel.Value)
                    showLabel = false;
                else
                    showLabel = null;
                
                // Play click sound
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
        }
        
        private void DrawPositionControls(Rect rect)
        {
            // Draw bordered background like other controls
            Verse.Widgets.DrawAtlas(rect, TexUI.FloatMenuOptionBG);
            
            // Get current offset values
            float xOffset = positionOffset?.x ?? 0f;
            float yOffset = positionOffset?.y ?? 0f;
            
            float inset = 5f;
            float buttonSize = 22f;
            float valueWidth = 35f;
            float labelWidth = 20f;
            float resetButtonWidth = 45f;
            
            // Calculate available width for controls (excluding reset button)
            float availableWidth = rect.width - (resetButtonWidth + inset * 3);
            
            // First row: X controls (compact layout)
            float row1Y = rect.y + inset;
            float startX = rect.x + inset;
            
            // X label
            Rect xLabelRect = new Rect(startX, row1Y, labelWidth, buttonSize);
            Text.Anchor = TextAnchor.MiddleLeft;
            Verse.Widgets.Label(xLabelRect, "X:");
            
            // X minus button
            Rect xMinusRect = new Rect(startX + labelWidth + 2f, row1Y, buttonSize, buttonSize);
            if (Verse.Widgets.ButtonText(xMinusRect, "-", true, false, true))
            {
                Vector2 current = positionOffset ?? Vector2.zero;
                current.x = Mathf.Clamp(current.x - 1f, -10f, 10f);
                positionOffset = current;
                SoundDefOf.Click.PlayOneShotOnCamera();
                ApplyChanges();
            }
            
            // X value display with background
            Rect xValueBgRect = new Rect(startX + labelWidth + buttonSize + 4f, row1Y, valueWidth, buttonSize);
            GUI.color = new Color(1f, 1f, 1f, 0.05f);
            Verse.Widgets.DrawTextureFitted(xValueBgRect, BaseContent.WhiteTex, 1f);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.gray;
            Verse.Widgets.Label(xValueBgRect, xOffset.ToString("F0"));
            GUI.color = Color.white;
            
            // X plus button
            Rect xPlusRect = new Rect(startX + labelWidth + buttonSize + valueWidth + 6f, row1Y, buttonSize, buttonSize);
            if (Verse.Widgets.ButtonText(xPlusRect, "+", true, false, true))
            {
                Vector2 current = positionOffset ?? Vector2.zero;
                current.x = Mathf.Clamp(current.x + 1f, -10f, 10f);
                positionOffset = current;
                SoundDefOf.Click.PlayOneShotOnCamera();
                ApplyChanges();
            }
            
            // Second row: Y controls (compact layout)
            float row2Y = rect.y + inset + buttonSize + 3f;
            
            // Y label
            Rect yLabelRect = new Rect(startX, row2Y, labelWidth, buttonSize);
            Text.Anchor = TextAnchor.MiddleLeft;
            Verse.Widgets.Label(yLabelRect, "Y:");
            
            // Y minus button
            Rect yMinusRect = new Rect(startX + labelWidth + 2f, row2Y, buttonSize, buttonSize);
            if (Verse.Widgets.ButtonText(yMinusRect, "-", true, false, true))
            {
                Vector2 current = positionOffset ?? Vector2.zero;
                current.y = Mathf.Clamp(current.y - 1f, -10f, 10f);
                positionOffset = current;
                SoundDefOf.Click.PlayOneShotOnCamera();
                ApplyChanges();
            }
            
            // Y value display with background
            Rect yValueBgRect = new Rect(startX + labelWidth + buttonSize + 4f, row2Y, valueWidth, buttonSize);
            GUI.color = new Color(1f, 1f, 1f, 0.05f);
            Verse.Widgets.DrawTextureFitted(yValueBgRect, BaseContent.WhiteTex, 1f);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = Color.gray;
            Verse.Widgets.Label(yValueBgRect, yOffset.ToString("F0"));
            GUI.color = Color.white;
            
            // Y plus button
            Rect yPlusRect = new Rect(startX + labelWidth + buttonSize + valueWidth + 6f, row2Y, buttonSize, buttonSize);
            if (Verse.Widgets.ButtonText(yPlusRect, "+", true, false, true))
            {
                Vector2 current = positionOffset ?? Vector2.zero;
                current.y = Mathf.Clamp(current.y + 1f, -10f, 10f);
                positionOffset = current;
                SoundDefOf.Click.PlayOneShotOnCamera();
                ApplyChanges();
            }
            
            // Reset button on the right side, vertically centered
            Rect resetRect = new Rect(rect.xMax - resetButtonWidth - inset, rect.y + rect.height/2 - buttonSize/2, resetButtonWidth, buttonSize);
            if (Verse.Widgets.ButtonText(resetRect, "Reset", true, false, true))
            {
                positionOffset = null;
                SoundDefOf.Click.PlayOneShotOnCamera();
                ApplyChanges();
            }
            
            Text.Anchor = TextAnchor.UpperLeft;
        }
    }
}