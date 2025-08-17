using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace LabelsOnFloor.FontLibrary
{
    public class JetBrainsMonoFont : IFont
    {
        private FontAtlas _atlas;
        private Texture2D _texture;
        private Texture2D _previewTexture;
        private readonly Dictionary<int, Material> _materialCache = new Dictionary<int, Material>();
        private bool _initialized = false;

        public string Name => "JetBrainsMono";
        public Texture2D PreviewTexture => _previewTexture;

        public void Initialize()
        {
            if (_initialized)
                return;

            try
            {
                // Try to load from new directory structure first
                _texture = ContentFinder<Texture2D>.Get("Fonts/JetBrainsMono/Font", false);
                _previewTexture = ContentFinder<Texture2D>.Get("Fonts/JetBrainsMono/Preview", false);
                
                if (_texture == null)
                {
                    // Try old location
                    _texture = ContentFinder<Texture2D>.Get("JetBrainsMono", false);
                }
                
                if (_texture == null)
                {
                    // Fall back to ConsolasExtended
                    _texture = ContentFinder<Texture2D>.Get("ConsolasExtended", false);
                }

                if (_texture == null)
                {
                    // Fall back to original Consolas
                    _texture = ContentFinder<Texture2D>.Get("Consolas", false);
                }

                if (_texture == null)
                {
                    Log.Error("[LabelsOnFloor] No font texture found! Labels will not display correctly.");
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
                    // Grid layout - JetBrainsMono or ConsolasExtended
                    gridWidth = 16;
                    gridHeight = 16;
                    characterMapping = BuildCharacterMapping();
                }
                
                _atlas = new FontAtlas(_texture, gridWidth, gridHeight, characterMapping, flipVertical: true);
                
                _initialized = true;
            }
            catch (Exception e)
            {
                Log.Error($"[LabelsOnFloor] Failed to initialize JetBrainsMonoFont: {e.Message}\n{e.StackTrace}");
            }
        }

        private Dictionary<char, int> BuildCharacterMapping()
        {
            var mapping = new Dictionary<char, int>();

            mapping[' '] = 0;
            
            for (int i = 33; i <= 126; i++)
            {
                mapping[(char)i] = i - 32;
            }
            
            for (int i = 160; i <= 255; i++)
            {
                mapping[(char)i] = i - 65;
            }
            
            for (int i = 256; i <= 319; i++)
            {
                int index = i - 65;
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