using UnityEngine;

namespace LabelsOnFloor.FontLibrary
{
    public struct CharacterGlyph
    {
        public readonly char Character;
        public readonly Vector2 UvBottomLeft;
        public readonly Vector2 UvTopRight;
        public readonly float Width;
        public readonly float Height;
        public readonly bool IsValid;

        public CharacterGlyph(char character, Vector2 uvBottomLeft, Vector2 uvTopRight, float width, float height)
        {
            Character = character;
            UvBottomLeft = uvBottomLeft;
            UvTopRight = uvTopRight;
            Width = width;
            Height = height;
            IsValid = true;
        }

        public static CharacterGlyph Invalid => new CharacterGlyph();

        public Vector2 UvBottomRight => new Vector2(UvTopRight.x, UvBottomLeft.y);
        public Vector2 UvTopLeft => new Vector2(UvBottomLeft.x, UvTopRight.y);
    }
}