using UnityEngine;

namespace LabelsOnFloor.FontLibrary
{
    public interface IFont
    {
        string Name { get; }
        bool SupportsCharacter(char character);
        CharacterGlyph GetGlyph(char character);
        Material GetMaterial(Color color, float opacity);
        void Initialize();
        Texture2D PreviewTexture { get; }
    }
}