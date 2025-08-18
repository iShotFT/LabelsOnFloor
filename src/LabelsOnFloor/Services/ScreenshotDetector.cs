using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;

namespace LabelsOnFloor.Services
{
    /// <summary>
    /// Service for detecting when screenshot/time-lapse mods are actively capturing images.
    /// Supports Progress Renderer family and native RimWorld screenshot mode.
    /// </summary>
    public static class ScreenshotDetector
    {
        /// <summary>
        /// Dictionary of supported Progress Renderer mod variants.
        /// Key: Package ID, Value: Display name
        /// </summary>
        private static readonly Dictionary<string, string> CompatibleMods = new Dictionary<string, string>
        {
            { "neptimus7.progressrenderer", "Neptune's Progress Renderer" },
            { "Jaxe.Progress.Renderer", "Progress Renderer (Original)" },
            { "Mlie.ProgressRenderer", "Progress Renderer (Mlie)" },
            { "community.progress.renderer", "Community Progress Renderer" }
        };

        private static bool _initialized = false;
        private static bool _progressRendererAvailable = false;
        private static List<string> _detectedCompatibleMods = new List<string>();

        /// <summary>
        /// Initialize the screenshot detector. Should be called once at mod startup.
        /// </summary>
        public static void Initialize()
        {
            if (_initialized)
                return;

            try
            {
                DetectCompatibleMods();
                _progressRendererAvailable = DetectProgressRenderer();
                _initialized = true;

#if DEBUG
                if (_detectedCompatibleMods.Count > 0)
                {
                    Log.Message($"[LabelsOnFloor] Screenshot compatibility detected: {string.Join(", ", _detectedCompatibleMods)}");
                }
#endif
            }
            catch (Exception ex)
            {
                Log.Error($"[LabelsOnFloor] Error initializing screenshot detector: {ex}");
                _initialized = true; // Mark as initialized to prevent repeated failures
            }
        }

        /// <summary>
        /// Detect which compatible mods are currently loaded.
        /// </summary>
        private static void DetectCompatibleMods()
        {
            _detectedCompatibleMods.Clear();
            
            var activeMods = ModsConfig.ActiveModsInLoadOrder?.ToList();
            if (activeMods == null) return;

            foreach (var mod in activeMods)
            {
                var packageId = mod.PackageId?.ToLower();
                if (packageId != null && CompatibleMods.ContainsKey(packageId))
                {
                    _detectedCompatibleMods.Add(CompatibleMods[packageId]);
                }
            }
        }

        /// <summary>
        /// Detect if Progress Renderer classes are available via reflection.
        /// </summary>
        private static bool DetectProgressRenderer()
        {
            try
            {
                // Try to find the MapComponent_RenderManager class
                var renderManagerType = AccessTools.TypeByName("ProgressRenderer.MapComponent_RenderManager");
                return renderManagerType != null;
            }
            catch (Exception ex)
            {
                Log.Warning($"[LabelsOnFloor] Could not detect Progress Renderer via reflection: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if any screenshot/time-lapse operation is currently in progress.
        /// </summary>
        /// <returns>True if screenshot in progress, false otherwise</returns>
        public static bool IsScreenshotInProgress()
        {
            if (!_initialized)
                Initialize();

            try
            {
                // Check RimWorld native screenshot mode
                if (Find.UIRoot?.screenshotMode?.Active == true)
                    return true;

                // Check Progress Renderer (if available)
                if (_progressRendererAvailable && Find.CurrentMap != null)
                {
                    try
                    {
                        // Use reflection to get the MapComponent_RenderManager
                        var renderManagerType = AccessTools.TypeByName("ProgressRenderer.MapComponent_RenderManager");
                        if (renderManagerType != null)
                        {
                            var renderManager = Find.CurrentMap.GetComponent(renderManagerType);
                            if (renderManager != null)
                            {
                                // Get the Rendering property
                                var renderingProperty = AccessTools.Property(renderManagerType, "Rendering");
                                if (renderingProperty != null)
                                {
                                    var isRendering = (bool)renderingProperty.GetValue(renderManager);
                                    return isRendering;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Progress Renderer might have been unloaded or changed
                        Log.Warning($"[LabelsOnFloor] Progress Renderer detection failed: {ex.Message}");
                        _progressRendererAvailable = false; // Disable further attempts this session
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.Error($"[LabelsOnFloor] Error in screenshot detection: {ex}");
                return false; // Fail safely - don't hide labels on error
            }
        }

        /// <summary>
        /// Check if any compatible screenshot mods are loaded and should show compatibility settings.
        /// </summary>
        /// <returns>True if compatibility category should be shown</returns>
        public static bool ShouldShowCompatibilityCategory()
        {
            if (!_initialized)
                Initialize();

            return _detectedCompatibleMods.Count > 0 || HasNativeScreenshotSupport();
        }

        /// <summary>
        /// Get list of detected compatible mods for display in UI.
        /// </summary>
        /// <returns>List of detected mod display names</returns>
        public static List<string> GetDetectedCompatibleMods()
        {
            if (!_initialized)
                Initialize();

            var result = new List<string>(_detectedCompatibleMods);
            
            // Always add native RimWorld support
            if (HasNativeScreenshotSupport())
                result.Add("RimWorld Native Screenshots");

            return result;
        }

        /// <summary>
        /// Check if native RimWorld screenshot support is available.
        /// </summary>
        /// <returns>Always true - native support is always available</returns>
        private static bool HasNativeScreenshotSupport()
        {
            return true; // RimWorld's screenshot mode is always available
        }

        /// <summary>
        /// Get a user-friendly description of detected mods.
        /// </summary>
        /// <returns>String describing detected compatible mods</returns>
        public static string GetCompatibilityDescription()
        {
            var detectedMods = GetDetectedCompatibleMods();
            if (detectedMods.Count == 0)
                return "No compatible mods detected";

            if (detectedMods.Count == 1)
                return $"Compatible with: {detectedMods[0]}";

            return $"Compatible with: {string.Join(", ", detectedMods)}";
        }
    }
}