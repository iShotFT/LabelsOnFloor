using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace LabelsOnFloor.FontLibrary
{
    public class FontAtlas
    {
        private readonly Texture2D _texture;
        private readonly int _gridWidth;
        private readonly int _gridHeight;
        private readonly float _cellWidth;
        private readonly float _cellHeight;
        private readonly Dictionary<char, int> _characterToIndex;
        private readonly bool _flipVertical;

        public Texture2D Texture => _texture;
        public int GridWidth => _gridWidth;
        public int GridHeight => _gridHeight;

        public FontAtlas(
            Texture2D texture, 
            int gridWidth, 
            int gridHeight,
            Dictionary<char, int> characterMapping,
            bool flipVertical = true)
        {
            _texture = texture ?? throw new ArgumentNullException(nameof(texture));
            _gridWidth = gridWidth;
            _gridHeight = gridHeight;
            _cellWidth = 1f / gridWidth;
            _cellHeight = 1f / gridHeight;
            _characterToIndex = characterMapping ?? throw new ArgumentNullException(nameof(characterMapping));
            _flipVertical = flipVertical;
        }

        public bool HasCharacter(char character)
        {
            return _characterToIndex.ContainsKey(character);
        }

        public CharacterGlyph GetGlyph(char character)
        {
            if (!_characterToIndex.TryGetValue(character, out int index))
            {
                if (_characterToIndex.TryGetValue(' ', out int spaceIndex))
                {
                    index = spaceIndex;
                }
                else
                {
                    return CharacterGlyph.Invalid;
                }
            }

            int gridX = index % _gridWidth;
            int gridY = index / _gridWidth;

            // Industry standard: 2-4 pixel padding to prevent texture bleeding
            // This is critical for texture atlases to prevent adjacent characters from bleeding
            // Dynamic padding: 2px for 560x1024, 4px for 1120x2048 (scales with resolution)
            float paddingPixels = _texture.width > 600 ? 4.0f : 2.0f;  // Auto-detect resolution
            float uvPadding = paddingPixels / _texture.width;
            float uvVPadding = paddingPixels / _texture.height;
            
            float uvLeft = gridX * _cellWidth + uvPadding;
            float uvRight = (gridX + 1) * _cellWidth - uvPadding;
            float uvBottom, uvTop;

            // Special handling for single-row textures
            if (_gridHeight == 1)
            {
                // For single row, no vertical flipping needed, no vertical padding
                uvBottom = 0f;
                uvTop = 1f;
            }
            else if (_flipVertical)
            {
                // Flip coordinates and apply padding
                uvTop = 1f - (gridY * _cellHeight) - uvVPadding;
                uvBottom = 1f - ((gridY + 1) * _cellHeight) + uvVPadding;
            }
            else
            {
                // Normal coordinates with padding
                uvBottom = gridY * _cellHeight + uvVPadding;
                uvTop = (gridY + 1) * _cellHeight - uvVPadding;
            }

            return new CharacterGlyph(
                character,
                new Vector2(uvLeft, uvBottom),
                new Vector2(uvRight, uvTop),
                1f,
                2.0f  // Match original MeshHandler height (size.y = 2.0f)
            );
        }

        public static FontAtlas CreateStandardAsciiAtlas(Texture2D texture, int gridWidth = 16, int gridHeight = 16)
        {
            var characterMapping = new Dictionary<char, int>();
            
            for (int i = 32; i <= 126; i++)
            {
                characterMapping[(char)i] = i - 32;
            }
            
            for (int i = 160; i <= 255; i++)
            {
                characterMapping[(char)i] = i - 32;
            }
            
            for (int i = 256; i <= 319; i++)
            {
                if (i - 32 < gridWidth * gridHeight)
                {
                    characterMapping[(char)i] = i - 32;
                }
            }

            return new FontAtlas(texture, gridWidth, gridHeight, characterMapping, flipVertical: true);
        }
    }
}