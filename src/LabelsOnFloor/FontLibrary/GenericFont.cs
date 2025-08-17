using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace LabelsOnFloor.FontLibrary
{
    /// <summary>
    /// Generic font implementation that can load any font from the Fonts/[FontName]/ directory structure
    /// </summary>
    public class GenericFont : IFont
    {
        private readonly string _fontName;
        private FontAtlas _atlas;
        private Texture2D _texture;
        private Texture2D _previewTexture;
        private readonly Dictionary<int, Material> _materialCache = new Dictionary<int, Material>();
        private bool _initialized = false;

        public string Name => _fontName;
        public Texture2D PreviewTexture => _previewTexture;

        public GenericFont(string fontName)
        {
            _fontName = fontName;
        }

        public void Initialize()
        {
            if (_initialized)
                return;

            try
            {
                // Try to load from new directory structure first
                _texture = ContentFinder<Texture2D>.Get($"Fonts/{_fontName}/Font", false);
                
                if (_texture != null)
                {
                    _previewTexture = ContentFinder<Texture2D>.Get($"Fonts/{_fontName}/Preview", false);
                }
                else
                {
                    // Try old location as fallback
                    _texture = ContentFinder<Texture2D>.Get(_fontName, false);
                }

                if (_texture == null)
                {
                    // Try even more variations
                    // Try without "Font" suffix
                    _texture = ContentFinder<Texture2D>.Get($"Fonts/{_fontName}/{_fontName}", false);
                    
                    // Try in root Fonts folder
                    if (_texture == null)
                    {
                        _texture = ContentFinder<Texture2D>.Get($"Fonts/{_fontName}", false);
                    }
                }

                if (_texture == null)
                {
                    Log.Error($"[LabelsOnFloor] No font texture found for {_fontName} after trying all paths!");
                    return;
                }

                // Detect texture layout based on dimensions
                Dictionary<char, int> characterMapping;
                int gridWidth, gridHeight;
                
                if (_texture.width > 2000) // Original Consolas.png is 2415px wide (single row)
                {
                    // Single row layout - original Consolas
                    gridWidth = 69; // 69 characters in the original
                    gridHeight = 1;
                    characterMapping = BuildOriginalConsolasMapping();
                }
                else
                {
                    // Grid layout - new font system
                    gridWidth = 16;
                    gridHeight = 16;
                    characterMapping = BuildCharacterMapping();
                }
                
                _atlas = new FontAtlas(_texture, gridWidth, gridHeight, characterMapping, flipVertical: true);
                
                _initialized = true;
            }
            catch (Exception e)
            {
                Log.Error($"[LabelsOnFloor] Failed to initialize {_fontName} font: {e.Message}\n{e.StackTrace}");
            }
        }

        private Dictionary<char, int> BuildCharacterMapping()
        {
            var mapping = new Dictionary<char, int>();

            mapping[' '] = 0;
            
            // Basic ASCII (33-126)
            for (int i = 33; i <= 126; i++)
            {
                mapping[(char)i] = i - 32;
            }
            
            // Latin-1 Supplement (160-255)
            for (int i = 160; i <= 255; i++)
            {
                mapping[(char)i] = i - 65;
            }
            
            // Cyrillic (1040-1103) - Russian alphabet
            // Maps to indices 191-254
            for (int i = 1040; i <= 1103; i++)
            {
                int index = 191 + (i - 1040);
                if (index < 256)
                {
                    mapping[(char)i] = index;
                }
            }

            return mapping;
        }

        private Dictionary<char, int> BuildOriginalConsolasMapping()
        {
            var mapping = new Dictionary<char, int>();
            
            // Original Consolas.png has special mapping
            mapping[' '] = 0;
            
            // Numbers and uppercase: ASCII 33-96 -> indices 1-64
            for (int i = 33; i <= 96; i++)
            {
                mapping[(char)i] = i - 32;
            }
            
            // Lowercase: ASCII 97-122 -> indices 39-64
            for (int i = 97; i <= 122; i++)
            {
                mapping[(char)i] = i - 58;
            }
            
            // Special characters at the end
            mapping['{'] = 65;
            mapping['|'] = 66;
            mapping['}'] = 67;
            mapping['~'] = 68;
            
            return mapping;
        }

        public bool SupportsCharacter(char character)
        {
            if (_atlas == null)
                return false;
            
            return _atlas.HasCharacter(character);
        }

        public CharacterGlyph GetGlyph(char character)
        {
            if (_atlas == null)
                return CharacterGlyph.Invalid;
            
            return _atlas.GetGlyph(character);
        }

        public Material GetMaterial(Color color, float opacity)
        {
            if (_texture == null)
                return null;

            color.a = opacity;
            int colorHash = color.GetHashCode();

            if (_materialCache.TryGetValue(colorHash, out var material))
            {
                return material;
            }

            material = MaterialPool.MatFrom(_texture, ShaderDatabase.Transparent, color);
            _materialCache[colorHash] = material;

            return material;
        }
    }
}