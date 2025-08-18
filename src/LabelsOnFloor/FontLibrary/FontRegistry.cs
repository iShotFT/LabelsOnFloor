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
                
                var allTextures = ContentFinder<Texture2D>.GetAllInFolder("Fonts");
                Log.Message($"[LabelsOnFloor] First GetAllInFolder returned {allTextures?.Count() ?? 0} textures");
                
                foreach (var texture in allTextures)
                {
                    if (texture != null)
                    {
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
                                Log.Message($"[LabelsOnFloor] FIRST PASS: Found font '{fontName}' from texture '{texture.name}'");
                            }
                        }
                        // Pattern 2: Might be just "Font" if in subdirectory
                        else if (texture.name == "Font" || texture.name.EndsWith("Font"))
                        {
                            // Log.Message($"[LabelsOnFloor] Texture name is 'Font' pattern, needs alternate detection");
                        }
                        
                        if (!string.IsNullOrEmpty(fontName))
                        {
                            // Log.Message($"[LabelsOnFloor] Attempting to register font: '{fontName}'");
                            
                            var font = new GenericFont(fontName);
                            font.Initialize();
                            RegisterFont(fontName, font);
                            registeredFonts.Add(fontName);
                            fontTextureCount++;
                            
                            // Log.Message($"[LabelsOnFloor] Successfully registered font: '{fontName}'");
                            
                            // Set JetBrainsMono as default if found, otherwise use first font
                            if (fontName == "JetBrainsMono" || _defaultFont == null)
                            {
                                _defaultFont = font;
                                // Log.Message($"[LabelsOnFloor] Set as default font: '{fontName}'");
                            }
                        }
                    }
                }
                
                // Dynamic font discovery - properly scan the Fonts folder
                // Each font has structure: Fonts/[FontName]/Font.png, Atlas.json, Preview.png
                
                Log.Message($"[LabelsOnFloor] Starting SECOND PASS font discovery...");
                
                // Use GetAllInFolder to find all textures in Fonts subdirectories
                var allTexturesSecond = ContentFinder<Texture2D>.GetAllInFolder("Fonts");
                Log.Message($"[LabelsOnFloor] Second GetAllInFolder returned {allTexturesSecond?.Count() ?? 0} textures");
                
                var processedFonts = new HashSet<string>();
                
                foreach (var texture in allTexturesSecond)
                {
                    if (texture == null || string.IsNullOrEmpty(texture.name)) 
                    {
                        // Log.Message($"[LabelsOnFloor] Skipping null or nameless texture");
                        continue;
                    }
                    
                    Log.Message($"[LabelsOnFloor] SECOND PASS: Processing texture: '{texture.name}'");
                    
                    // Parse the texture path to extract font name
                    // texture.name could be "Fonts/JetBrainsMono/Font" or similar
                    string[] pathParts = texture.name.Split('/');
                    // Log.Message($"[LabelsOnFloor] Path parts: {string.Join(", ", pathParts)}");
                    
                    // We expect at least Fonts/[FontName]/[FileName]
                    if (pathParts.Length >= 3 && pathParts[0] == "Fonts")
                    {
                        string fontName = pathParts[1];
                        Log.Message($"[LabelsOnFloor] SECOND PASS: Found potential font directory: '{fontName}'");
                        
                        // Skip if already processed
                        if (processedFonts.Contains(fontName) || _registeredFonts.ContainsKey(fontName))
                        {
                            // Log.Message($"[LabelsOnFloor] Font '{fontName}' already processed, skipping");
                            continue;
                        }
                        
                        // Check if this directory has the required Font.png file
                        // Log.Message($"[LabelsOnFloor] Checking for Font.png at: Fonts/{fontName}/Font");
                        var fontTexture = ContentFinder<Texture2D>.Get($"Fonts/{fontName}/Font", false);
                        if (fontTexture != null)
                        {
                            // Log.Message($"[LabelsOnFloor] Found valid font directory, registering: '{fontName}'");
                            // Found a valid font directory
                            var font = new GenericFont(fontName);
                            font.Initialize();
                            RegisterFont(fontName, font);
                            registeredFonts.Add(fontName);
                            processedFonts.Add(fontName);
                            fontTextureCount++;
                            // Log.Message($"[LabelsOnFloor] Successfully registered font: '{fontName}'");
                        }
                        else
                        {
                            // Log.Message($"[LabelsOnFloor] No Font.png found for: '{fontName}'");
                        }
                    }
                    else
                    {
                        // Log.Message($"[LabelsOnFloor] Path doesn't match expected pattern: '{texture.name}'");
                    }
                }
                
                // Set default font: Classic if available, otherwise first registered
                Log.Message($"[LabelsOnFloor] Total registered fonts after both passes: {_registeredFonts.Count}");
                Log.Message($"[LabelsOnFloor] Registered font names: {string.Join(", ", _registeredFonts.Keys)}");
                
                if (_registeredFonts.ContainsKey("Classic"))
                {
                    _defaultFont = _registeredFonts["Classic"];
                    // Log.Message($"[LabelsOnFloor] Set Classic as default font");
                }
                else if (_registeredFonts.Count > 0)
                {
                    // If no default set, use the first registered font
                    _defaultFont = _registeredFonts.Values.First();
                    // Log.Message($"[LabelsOnFloor] Set first font as default: {_registeredFonts.Keys.First()}");
                }
                else
                {
                    Log.Warning($"[LabelsOnFloor] WARNING: No fonts were registered!");
                }

                // Log.Message($"[LabelsOnFloor] FontRegistry initialized successfully with {_registeredFonts.Count} fonts");
                
                // FALLBACK: If no fonts found, try direct paths
                if (_registeredFonts.Count == 0)
                {
                    Log.Warning($"[LabelsOnFloor] No fonts found via GetAllInFolder, trying direct paths...");
                    
                    string[] knownFonts = { "Classic", "JetBrainsMono", "JetBrainsMonoBold", "JetBrainsMonoLight", "Medieval", "Typewriter" };
                    foreach (var fontName in knownFonts)
                    {
                        // Log.Message($"[LabelsOnFloor] Trying direct path for: {fontName}");
                        var testTexture = ContentFinder<Texture2D>.Get($"Fonts/{fontName}/Font", false);
                        if (testTexture != null)
                        {
                            // Log.Message($"[LabelsOnFloor] Found font via direct path: {fontName}");
                            var font = new GenericFont(fontName);
                            font.Initialize();
                            RegisterFont(fontName, font);
                            
                            if (fontName == "Classic" || _defaultFont == null)
                            {
                                _defaultFont = font;
                            }
                        }
                        else
                        {
                            // Log.Message($"[LabelsOnFloor] Direct path failed for: {fontName}");
                        }
                    }
                    
                    // Log.Message($"[LabelsOnFloor] After fallback, registered fonts: {_registeredFonts.Count}");
                }
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
            EnsureInitialized();
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