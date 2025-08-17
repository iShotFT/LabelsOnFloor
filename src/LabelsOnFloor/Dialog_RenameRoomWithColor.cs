using System.Linq;
using UnityEngine;
using Verse;
using LabelsOnFloor.SettingsLibrary.Widgets;

namespace LabelsOnFloor
{
    public class Dialog_RenameRoomWithColor : Window
    {
        private readonly CustomRoomData _customRoomData;
        
        private string curName;
        private string originalName;
        private Color? selectedColor;
        private bool focusedNameField;
        private int startAcceptingInputAtFrame;
        private bool hasSelectedText;
        
        // Standard RimWorld dialog sizing
        private const float LabelWidth = 100f;
        private const float FieldSpacing = 10f;
        private const float RowHeight = 30f;
        private const float ButtonHeight = 30f;
        
        public override Vector2 InitialSize => new Vector2(380f, 200f);
        
        public Dialog_RenameRoomWithColor(CustomRoomData customRoomData)
        {
            _customRoomData = customRoomData;
            
            // Get the original room name for the title and default value
            originalName = GetRoomDisplayName(customRoomData);
            
            // If there's a custom label, use it; otherwise use the original name
            // Always show in uppercase
            curName = !string.IsNullOrEmpty(customRoomData.Label) ? customRoomData.Label.ToUpper() : originalName.ToUpper();
            selectedColor = customRoomData.CustomColor;
            
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
            string title = "FALCLF.RenameRoom".Translate() + ": " + originalName;
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
                    curName = tempName;
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
                }
            );
            
            // Bottom buttons - RimWorld standard style
            float buttonWidth = (inRect.width - 10f) / 2f;
            float buttonY = inRect.yMax - ButtonHeight;
            
            // Cancel button (left)
            if (Verse.Widgets.ButtonText(new Rect(0f, buttonY, buttonWidth, ButtonHeight), "CancelButton".Translate()))
            {
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
        
        private void ApplyChanges()
        {
            // Trim whitespace and ensure label is stored in uppercase
            _customRoomData.Label = curName?.Trim().ToUpper() ?? "";
            _customRoomData.CustomColor = selectedColor;
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