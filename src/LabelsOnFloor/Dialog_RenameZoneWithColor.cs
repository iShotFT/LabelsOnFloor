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
        private bool focusedNameField;
        private int startAcceptingInputAtFrame;
        private bool hasSelectedText;
        
        // Standard RimWorld dialog sizing
        private const float LabelWidth = 100f;
        private const float FieldSpacing = 10f;
        private const float RowHeight = 30f;
        private const float ButtonHeight = 30f;
        
        public override Vector2 InitialSize => new Vector2(380f, 240f);
        
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
            
            curY += RowHeight + 15f;
            
            // Bottom buttons (Cancel and OK)
            float buttonWidth = (inRect.width - 10f) / 2f;
            Rect cancelButtonRect = new Rect(0f, inRect.height - ButtonHeight, buttonWidth, ButtonHeight);
            Rect okButtonRect = new Rect(inRect.width - buttonWidth, inRect.height - ButtonHeight, buttonWidth, ButtonHeight);
            
            if (Verse.Widgets.ButtonText(cancelButtonRect, "Cancel".Translate()))
            {
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
    }
}