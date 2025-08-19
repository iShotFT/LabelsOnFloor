using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.Sound;
using RimWorld;
using LabelsOnFloor.SettingsLibrary.Widgets;

namespace LabelsOnFloor
{
    public class Dialog_RenameRoomWithColor : Window
    {
        private readonly CustomRoomData _customRoomData;
        
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
        
        public Dialog_RenameRoomWithColor(CustomRoomData customRoomData)
        {
            _customRoomData = customRoomData;
            
            // Get the original room name for the title and default value
            originalName = GetRoomDisplayName(customRoomData);
            
            // If there's a custom label, use it; otherwise use the original name
            // Always show in uppercase
            curName = !string.IsNullOrEmpty(customRoomData.Label) ? customRoomData.Label.ToUpper() : originalName.ToUpper();
            selectedColor = customRoomData.CustomColor;
            showLabel = customRoomData.ShowLabel;
            positionOffset = customRoomData.PositionOffset;
            
            // Store original values for cancel
            originalCustomLabel = customRoomData.Label;
            originalCustomColor = customRoomData.CustomColor;
            originalShowLabel = customRoomData.ShowLabel;
            originalPositionOffset = customRoomData.PositionOffset;
            
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
            string title = "FALCLF.RenameRoom".Translate();
            Verse.Widgets.Label(titleRect, title);
            Text.Font = GameFont.Small;
            
            float curY = titleRect.yMax + 15f;
            
            // Calculate field width based on available space
            float fieldWidth = inRect.width - LabelWidth - FieldSpacing;
            
            // Two-column layout for fields
            // Row 1: Room Name
            Rect nameLabelRect = new Rect(0f, curY, LabelWidth, RowHeight);
            Text.Anchor = TextAnchor.MiddleLeft;
            Verse.Widgets.Label(nameLabelRect, "FALCLF.RoomName".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            
            Rect nameFieldRect = new Rect(LabelWidth + FieldSpacing, curY, fieldWidth, RowHeight);
            GUI.SetNextControlName("RenameField");
            
            // Handle text field with proper selection for hotkey opening
            string tempName = curName;
            if (AcceptsInput)
            {
                tempName = Verse.Widgets.TextField(nameFieldRect, curName);
                // Force uppercase and limit length
                tempName = tempName.ToUpper();
                if (tempName.Length < 28) // Max name length from base Dialog_Rename
                {
                    if (curName != tempName)
                    {
                        curName = tempName;
                        // Live update the label
                        _customRoomData.Label = curName?.Trim().ToUpper() ?? "";
                        Main.Instance.LabelPlacementHandler.SetDirty();
                    }
                }
            }
            else
            {
                // Draw the field but don't accept input yet
                Verse.Widgets.TextField(nameFieldRect, curName);
            }
            
            // Auto-focus and select all text
            if (!focusedNameField)
            {
                UI.FocusControl("RenameField", this);
                focusedNameField = true;
            }
            
            // Select all text on the first frame where we're focused and accepting input
            if (focusedNameField && AcceptsInput && !hasSelectedText)
            {
                var textEditor = (UnityEngine.TextEditor)GUIUtility.GetStateObject(typeof(UnityEngine.TextEditor), GUIUtility.keyboardControl);
                if (textEditor != null && textEditor.text == curName)
                {
                    textEditor.SelectAll();
                    hasSelectedText = true;
                }
            }
            
            curY += RowHeight + 10f;
            
            // Row 2: Room Color
            Rect colorLabelRect = new Rect(0f, curY, LabelWidth, RowHeight);
            Text.Anchor = TextAnchor.MiddleLeft;
            Verse.Widgets.Label(colorLabelRect, "FALCLF.RoomColor".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            
            Rect colorFieldRect = new Rect(LabelWidth + FieldSpacing, curY, fieldWidth, RowHeight);
            ColorDropdownWidget.DrawColorDropdownButton(
                colorFieldRect,
                ref selectedColor,
                Main.Instance.GetDefaultLabelColor(),
                (color) => {
                    selectedColor = color;
                    // Live update the color
                    _customRoomData.CustomColor = selectedColor;
                    Main.Instance.LabelPlacementHandler.SetDirty();
                }
            );
            
            curY += RowHeight + 10f;
            
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
            
            // Bottom buttons - RimWorld standard style
            float buttonWidth = (inRect.width - 10f) / 2f;
            float buttonY = inRect.yMax - ButtonHeight;
            
            // Cancel button (left)
            if (Verse.Widgets.ButtonText(new Rect(0f, buttonY, buttonWidth, ButtonHeight), "CancelButton".Translate()))
            {
                // Revert to original values
                _customRoomData.Label = originalCustomLabel;
                _customRoomData.CustomColor = originalCustomColor;
                _customRoomData.ShowLabel = originalShowLabel;
                _customRoomData.PositionOffset = originalPositionOffset;
                Main.Instance.LabelPlacementHandler.SetDirty();
                Close();
            }
            
            // OK button (right)
            if (Verse.Widgets.ButtonText(new Rect(buttonWidth + 10f, buttonY, buttonWidth, ButtonHeight), "OK".Translate()))
            {
                ApplyChanges();
                Close();
            }
            
            // Handle Enter key to accept
            if (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
            {
                ApplyChanges();
                Close();
                Event.current.Use();
            }
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
                
                // Live update visibility
                _customRoomData.ShowLabel = showLabel;
                Main.Instance.LabelPlacementHandler.SetDirty();
                
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
                _customRoomData.PositionOffset = positionOffset;
                Main.Instance.LabelPlacementHandler.SetDirty();
                SoundDefOf.Click.PlayOneShotOnCamera();
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
                _customRoomData.PositionOffset = positionOffset;
                Main.Instance.LabelPlacementHandler.SetDirty();
                SoundDefOf.Click.PlayOneShotOnCamera();
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
                _customRoomData.PositionOffset = positionOffset;
                Main.Instance.LabelPlacementHandler.SetDirty();
                SoundDefOf.Click.PlayOneShotOnCamera();
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
                _customRoomData.PositionOffset = positionOffset;
                Main.Instance.LabelPlacementHandler.SetDirty();
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
            
            // Reset button on the right side, vertically centered
            Rect resetRect = new Rect(rect.xMax - resetButtonWidth - inset, rect.y + rect.height/2 - buttonSize/2, resetButtonWidth, buttonSize);
            if (Verse.Widgets.ButtonText(resetRect, "Reset", true, false, true))
            {
                positionOffset = null;
                _customRoomData.PositionOffset = positionOffset;
                Main.Instance.LabelPlacementHandler.SetDirty();
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
            
            Text.Anchor = TextAnchor.UpperLeft;
        }
        
        private void ApplyChanges()
        {
            // Trim whitespace and ensure label is stored in uppercase
            _customRoomData.Label = curName?.Trim().ToUpper() ?? "";
            _customRoomData.CustomColor = selectedColor;
            _customRoomData.ShowLabel = showLabel;
            _customRoomData.PositionOffset = positionOffset;
            Main.Instance.LabelPlacementHandler.SetDirty();
        }
        
        private string GetRoomDisplayName(CustomRoomData customRoomData)
        {
            if (customRoomData.RoomObject == null)
                return "Room";
            
            // Use the same logic as LabelMaker to get the default room name
            // This ensures consistency with what's displayed on the floor
            return customRoomData.RoomObject.Role?.label ?? "Room";
        }
    }
}