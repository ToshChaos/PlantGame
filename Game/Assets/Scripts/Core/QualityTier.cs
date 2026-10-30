using UnityEngine;

namespace PlantGame.Core
{
    /// <summary>
    /// T63: graphics tiers. Low / Medium / High map to the quality levels of the same name (QualityTierBuilder gives each its URP asset:
    /// High = desktop, Medium = a 2022 phone at 1080 x 2340: 0.75 render scale, no HDR, short 1024 shadows; Low = 0.6 scale, no shadows).
    /// The level switches as soon as GameSettings.Quality changes; WorldOptimizer reads Lite / Thin when a world is built.
    /// </summary>
    public static class QualityTier
    {
        public const int Low = 0, Medium = 1, High = 2;
        public static readonly string[] Names = { "Low", "Medium", "High" };

        /// <summary>The world pass runs (static batching, low-poly spheres, small props cast no shadows).</summary>
        public static bool Lite => GameSettings.Quality < High;
        /// <summary>Half of the small decoration is hidden (rendering only: colliders and layout stay, so seed codes are unaffected).</summary>
        public static bool Thin => GameSettings.Quality == Low;

        const string PresetKey = "plantgame.preset";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (!PlayerPrefs.HasKey(PresetKey)) DetectPreset();
            Apply();
            GameSettings.Changed += Apply;
        }

        /// <summary>
        /// First run on a phone or tablet: picks the tier, the 30 fps battery saver and postprocessing from the device and saves them, so the
        /// player's own changes in Options win afterwards. The reference target is a 2022 phone at 1080 x 2340 (Galaxy S22 / A53, Pixel 6a class):
        /// Medium, 30 fps, no postprocessing. Weak devices (under 5 GB, fewer than 6 cores or no Vulkan) get Low; only 12 GB devices keep 60 fps.
        /// Desktop keeps the defaults (High, 60+ fps, postprocessing on).
        /// </summary>
        static void DetectPreset()
        {
            PlayerPrefs.SetInt(PresetKey, 1);
            if (!Application.isMobilePlatform) { PlayerPrefs.Save(); return; }
            int ram = SystemInfo.systemMemorySize;   // MB
            bool vulkan = SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Vulkan || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Metal;
            bool weak = ram < 5000 || SystemInfo.processorCount < 6 || !vulkan;
            bool strong = ram >= 11500 && !weak;
            GameSettings.Quality = weak ? Low : Medium;
            GameSettings.Fps30 = !strong;
            GameSettings.Bloom = false;
            Debug.Log($"[PlantGame] First-run preset: {Names[GameSettings.Quality]}, {(GameSettings.Fps30 ? 30 : 60)} fps, postprocessing off ({ram} MB, {SystemInfo.processorCount} cores, {SystemInfo.graphicsDeviceType}, {SystemInfo.graphicsDeviceName}, {Screen.width} x {Screen.height}).");
        }

        static void Apply()
        {
            var name = Names[GameSettings.Quality];
            var names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++)
                if (names[i] == name) { if (QualitySettings.GetQualityLevel() != i) QualitySettings.SetQualityLevel(i, true); return; }
        }
    }
}
