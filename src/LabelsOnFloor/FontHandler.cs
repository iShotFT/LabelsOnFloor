using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace LabelsOnFloor
{
    public struct CharBoundsInTexture
    {
        public float Left, Right, Top, Bottom;
    }

    public class FontHandler
    {
        private float _charWidthAsTexturePortion = -1f;
        private Material _material;
        private Texture2D _fontTexture;
        private bool _isExtendedFont = false;
        private int _gridSize = 16; // Default 16x16, can be 32x32 for more chars

        public bool IsFontLoaded()
        {
            if (_fontTexture == null)
            {
                LoadFontTexture();
            }
            
            if (_fontTexture == null)
                return false;

            if (_charWidthAsTexturePortion < 0f)
            {
                // Original font has all characters in one row
                // Extended font would have a grid layout
                if (_isExtendedFont)
                {
                    // Detect grid size based on texture dimensions
                    if (_fontTexture.width == _fontTexture.height)
                    {
                        // Square texture, likely 32x32 grid for 1024 chars
                        _gridSize = 32;
                        _charWidthAsTexturePortion = 1f / 32f;
                    }
                    else
                    {
                        // Rectangular, assume 16x16 grid for 256 chars
                        _gridSize = 16;
                        _charWidthAsTexturePortion = 1f / 16f;
                    }
                }
                else
                {
                    // Original font texture width calculation
                    _charWidthAsTexturePortion = 35f / _fontTexture.width;
                }
            }

            return true;
        }
        
        private void LoadFontTexture()
        {
            // Try to load extended font texture first
            _fontTexture = ContentFinder<Texture2D>.Get("ConsolasExtended", false);
            
            if (_fontTexture != null)
            {
                _isExtendedFont = true;
            }
            else
            {
                // Fallback to original font
                _fontTexture = ContentFinder<Texture2D>.Get("Consolas", false);
                _isExtendedFont = false;
                
                if (_fontTexture == null)
                {
                    ModLog.Error("LabelsOnFloor: Could not load font texture");
                }
            }
        }

        public Material GetMaterial()
        {
            if (_material == null && IsFontLoaded())
            {
                var color = Main.Instance.GetDefaultLabelColor();
                color.a = Main.Instance.GetOpacity();
                _material = MaterialPool.MatFrom(_fontTexture, ShaderDatabase.Transparent, color);
            }

            return _material;
        }

        public void Reset()
        {
            _material = null;
        }

        public IEnumerable<CharBoundsInTexture> GetBoundsInTextureFor(string text)
        {
            foreach (char c in text)
            {
                yield return GetCharBoundsInTextureFor(c);
            }
        }

        private CharBoundsInTexture GetCharBoundsInTextureFor(char c)
        {
            var index = GetIndexInFontForChar(c);
            
            if (_isExtendedFont)
            {
                // Extended font uses a grid layout (16x16 or 32x32)
                int row = index / _gridSize;
                int col = index % _gridSize;
                
                float charWidthUV = 1f / _gridSize;  // Each character takes 1/16th or 1/32nd of texture
                float charHeightUV = 1f / _gridSize;
                
                float left = col * charWidthUV;
                float right = left + charWidthUV;
                float top = row * charHeightUV;
                float bottom = top + charHeightUV;
                
                return new CharBoundsInTexture()
                {
                    Left = left,
                    Right = right,
                    Top = top,
                    Bottom = bottom
                };
            }
            else
            {
                // Original font has all characters in a single row
                float left = index * _charWidthAsTexturePortion;
                
                return new CharBoundsInTexture()
                {
                    Left = left,
                    Right = left + _charWidthAsTexturePortion,
                    Top = 0f,
                    Bottom = 1f  // Use full height for single-row texture
                };
            }
        }

        private int GetIndexInFontForChar(char c)
        {
            int charCode = (int)c;
            
            if (_isExtendedFont)
            {
                // Extended font supports more characters
                if (charCode < 32)
                    return 0; // Control characters -> space
                    
                if (charCode <= 126)
                    return charCode - 32; // Basic ASCII (space to ~) [0-94]
                
                if (_gridSize == 32)
                {
                    // 32x32 grid = 1024 characters total
                    // Latin-1 Supplement (160-255) [95-190]
                    if (charCode >= 160 && charCode <= 255)
                        return 95 + (charCode - 160);
                    
                    // Latin Extended-A (256-383) [191-318]
                    if (charCode >= 256 && charCode <= 383)
                        return 191 + (charCode - 256);
                    
                    // Cyrillic (1024-1279) [319-574]
                    if (charCode >= 0x0400 && charCode <= 0x04FF)
                        return 319 + (charCode - 0x0400);
                    
                    // Additional space for more characters...
                }
                else
                {
                    // 16x16 grid = 256 characters
                    // Latin-1 Supplement (160-255) [95-190]
                    if (charCode >= 160 && charCode <= 255)
                        return 95 + (charCode - 160);
                    
                    // Latin Extended-A (256-319) - partial [191-254]
                    if (charCode >= 256 && charCode <= 319)
                        return 191 + (charCode - 256);
                }
                    
                // Fallback for unsupported characters
                return 63; // Question mark position
            }
            else
            {
                // Original font mapping - only basic ASCII
                if (charCode < 33)
                    return 0; // Space
                
                if (charCode < 97)
                    return charCode - 32; // Numbers, uppercase
                    
                if (charCode < 127)
                    return charCode - 58; // Lowercase
                    
                return 0; // Unsupported - show as space
            }
        }
    }
}