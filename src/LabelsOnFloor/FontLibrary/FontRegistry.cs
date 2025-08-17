using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace LabelsOnFloor.FontLibrary
{
    public static class FontRegistry
    {
        private static readonly Dictionary<string, IFont> _registeredFonts = new Dictionary<string, IFont>();
        private static IFont _defaultFont;
        private static bool _initialized = false;
        private static readonly object _initLock = new object();

        public static IFont DefaultFont
        {
            get
            {
                EnsureInitialized();
                return _defaultFont;
            }
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
                return;

            lock (_initLock)
            {
                if (_initialized)
                    return;

                Initialize();
            }
        }

        public static void Initialize()
        {
            if (_initialized)
                return;

            _initialized = true;

            try
            {
                // Dynamically discover all fonts in the Fonts directory
                // This uses RimWorld's content system to find textures at runtime
                var fontTextureCount = 0;
                var registeredFonts = new List<string>();
                
                // Try to find all Font.png textures in subdirectories
                // RimWorld's ContentFinder will look for textures in the mod's Textures folder
                
                foreach (var texture in ContentFinder<Texture2D>.GetAllInFolder("Fonts"))
                {
                    if (texture != null)
                    {
                        // Found texture in Fonts folder
                        
                        // The texture name could be in various formats
                        // Try to extract font name from different possible patterns
                        string fontName = null;
                        
                        // Pattern 1: "Fonts/FontName/Font" or just "FontName/Font"
                        if (texture.name.EndsWith("/Font"))
                        {
                            var pathParts = texture.name.Split('/');
                            if (pathParts.Length >= 2)
                            {
                                fontName = pathParts[pathParts.Length - 2];
                            }
                        }
                        // Pattern 2: Might be just "Font" if in subdirectory
                        else if (texture.name == "Font" || texture.name.EndsWith("Font"))
                        {
                            // This might not give us the font name directly
                            // Try to parse from the full path if available
                            // Texture name is just 'Font', trying alternate detection
                        }
                        
                        if (!string.IsNullOrEmpty(fontName))
                        {
                            // Discovered font
                            
                            var font = new GenericFont(fontName);
                            font.Initialize();
                            RegisterFont(fontName, font);
                            registeredFonts.Add(fontName);
                            fontTextureCount++;
                            
                            // Set JetBrainsMono as default if found, otherwise use first font
                            if (fontName == "JetBrainsMono" || _defaultFont == null)
                            {
                                _defaultFont = font;
                            }
                        }
                    }
                }
                
                // Always try direct approach as it's more reliable
                // This works better for discovering fonts in subdirectories
                
                // List of potential font names to check
                // This list should include all fonts we generate
                string[] potentialFonts = { 
                    "JetBrainsMono", 
                    "JetBrainsMonoBold", 
                    "JetBrainsMonoLight",
                    "Medieval",
                    "Consolas",
                    "ConsolasExtended"
                };
                
                foreach (var fontName in potentialFonts)
                {
                    // Skip if already registered
                    if (_registeredFonts.ContainsKey(fontName))
                    {
                        // Font already registered
                        continue;
                    }
                    
                    // Check if the font texture exists at the expected path
                    var testTexture = ContentFinder<Texture2D>.Get($"Fonts/{fontName}/Font", false);
                    if (testTexture != null)
                    {
                        // Found font via direct check
                        
                        var font = new GenericFont(fontName);
                        font.Initialize();
                        RegisterFont(fontName, font);
                        registeredFonts.Add(fontName);
                        fontTextureCount++;
                        
                        if (fontName == "JetBrainsMono" || _defaultFont == null)
                        {
                            _defaultFont = font;
                        }
                    }
                    else
                    {
                        // Font not found at expected path
                    }
                }

                if (_defaultFont == null && _registeredFonts.Count > 0)
                {
                    // If no default set, use the first registered font
                    _defaultFont = _registeredFonts.Values.First();
                }

                // FontRegistry initialized successfully
            }
            catch (Exception e)
            {
                Log.Error($"[LabelsOnFloor] Failed to initialize font registry: {e.Message}\n{e.StackTrace}");
                _defaultFont = null;
            }
        }

        public static void RegisterFont(string name, IFont font)
        {
            if (font == null)
                throw new ArgumentNullException(nameof(font));

            if (_registeredFonts.ContainsKey(name))
            {
                Log.Warning($"[LabelsOnFloor] Font '{name}' is already registered. Overwriting.");
            }

            _registeredFonts[name] = font;
            // Font registered successfully
        }

        public static IFont GetFont(string name)
        {
            EnsureInitialized();

            if (_registeredFonts.TryGetValue(name, out var font))
            {
                return font;
            }

            Log.Warning($"[LabelsOnFloor] Font '{name}' not found. Using default font.");
            return _defaultFont;
        }

        public static void SetDefaultFont(string name)
        {
            if (!_registeredFonts.TryGetValue(name, out var font))
            {
                Log.Error($"[LabelsOnFloor] Cannot set default font to '{name}' - font not registered.");
                return;
            }

            _defaultFont = font;
            // Default font updated
        }

        public static IEnumerable<string> GetRegisteredFontNames()
        {
            return _registeredFonts.Keys;
        }

        public static void Cleanup()
        {
            _registeredFonts.Clear();
            _defaultFont = null;
            _initialized = false;
        }
        
        public static void RefreshFonts()
        {
            // Store current font selection if any
            string currentFontName = (_defaultFont as GenericFont)?.Name;
            
            // Clear and reinitialize
            Cleanup();
            Initialize();
            
            // Try to restore previous selection
            if (!string.IsNullOrEmpty(currentFontName) && _registeredFonts.ContainsKey(currentFontName))
            {
                _defaultFont = _registeredFonts[currentFontName];
            }
            
            // Font registry refreshed
        }
    }
}