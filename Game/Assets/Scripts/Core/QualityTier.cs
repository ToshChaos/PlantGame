using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PlantGame.Core
{
    /// <summary>
    /// T63: graphics tiers. Low / Medium / High map to the quality levels of the same name (QualityTierBuilder gives each its URP asset:
    /// High = desktop, Medium = a 2022 phone at 1080 x 2340: 0.75 render scale, no HDR, short 1024 shadows; Low = 0.6 scale, no shadows;
    /// taller screens get a lower scale, see ScaleToScreen).
    /// The level switches as soon as GameSettings.Quality changes; WorldOptimizer reads Lite / Thin when a world is built.
    /// </summary>
    public static class QualityTier
    {
        public const int Low = 0, Medium = 1, High = 2;
        public static readonly string[] Names = { "Low", "Medium", "High" };

        /// <summary>The world pass runs (merged props, low-poly spheres, small props cast no shadows).</summary>
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
        /// player's own changes in Options win afterwards. The reference target is a 2022 phone at 1080 x 2340 (Galaxy S22 / A53, Pixel 6a class;
        /// also Galaxy S23, Honor 90 Pro): Medium, 30 fps, no postprocessing. Weak devices (under 5 GB or fewer than 6 cores) get Low.
        /// Desktop keeps the defaults (High, 60+ fps, postprocessing on).
        /// </summary>
        static void DetectPreset()
        {
            PlayerPrefs.SetInt(PresetKey, 1);
            if (!Application.isMobilePlatform) { PlayerPrefs.Save(); return; }
            int ram = SystemInfo.systemMemorySize;   // MB
            GameSettings.Quality = ram < 5000 || SystemInfo.processorCount < 6 ? Low : Medium;
            GameSettings.Fps30 = true;   // even 12 GB flagships (Honor 90 Pro) throttle at 60 fps after a few minutes
            GameSettings.Bloom = false;
            Debug.Log($"[PlantGame] First-run preset: {Names[GameSettings.Quality]}, {(GameSettings.Fps30 ? 30 : 60)} fps, postprocessing off ({ram} MB, {SystemInfo.processorCount} cores, {SystemInfo.graphicsDeviceType}, {SystemInfo.graphicsDeviceName}, {Screen.width} x {Screen.height}).");
        }

        static void Apply()
        {
            var name = Names[GameSettings.Quality];
            var names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++)
                if (names[i] == name) { if (QualitySettings.GetQualityLevel() != i) QualitySettings.SetQualityLevel(i, true); break; }
            ScaleToScreen();
        }

        /// <summary>
        /// Medium and Low render a fixed number of lines (810 / 650: 0.75 / 0.6 of the 1080-high reference phone), so a taller screen costs
        /// the same: 2700 x 1224 (Honor 90 Pro) renders at 0.66, 1440p at 0.56. Player only: in the editor it would rewrite the URP asset.
        /// </summary>
        static void ScaleToScreen()
        {
            if (Application.isEditor || GameSettings.Quality == High || !(QualitySettings.renderPipeline is UniversalRenderPipelineAsset urp)) return;
            float lines = GameSettings.Quality == Low ? 650f : 810f;
            urp.renderScale = Mathf.Clamp(lines / Mathf.Min(Screen.width, Screen.height), 0.5f, 1f);
        }
    }
}
