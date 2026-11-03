using System;
using UnityEngine;

namespace PlantGame.Core
{
    /// <summary>Player options persisted in PlayerPrefs (the pause menu toggles them). Minimal until the T19/T26 options screen.</summary>
    public static class GameSettings
    {
        const string HintsKey = "plantgame.hints", MusicKey = "plantgame.music", FollowKey = "plantgame.camfollow", SoundKey = "plantgame.sound", MusicVolKey = "plantgame.musicvol", SoundVolKey = "plantgame.soundvol", BloomKey = "plantgame.bloom", FpsKey = "plantgame.fps", AutoWaterKey = "plantgame.autowater", Fps30Key = "plantgame.fps30", QualityKey = "plantgame.quality";
        public static event Action Changed;

        /// <summary>On-screen key hints ("Q pick up / drop, E use, F water"). On by default.</summary>
        public static bool Hints
        {
            get => PlayerPrefs.GetInt(HintsKey, 0) == 1;
            set { PlayerPrefs.SetInt(HintsKey, value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        /// <summary>Difficulty of the last generated level (0 easy, 1 normal, 2 hard); the Generate screen opens on it.</summary>
        public static int Difficulty
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt("plantgame.difficulty", 1), 0, 2);
            set { PlayerPrefs.SetInt("plantgame.difficulty", Mathf.Clamp(value, 0, 2)); PlayerPrefs.Save(); }
        }

        /// <summary>Third-person camera turns with the player (A / D while moving forward steer it). On by default.</summary>
        public static bool CameraFollow
        {
            get => Application.isMobilePlatform || TouchControls || PlayerPrefs.GetInt(FollowKey, 1) == 1;   // touch: always on
            set { PlayerPrefs.SetInt(FollowKey, value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        /// <summary>Show the on-screen touch controls on a non-mobile device (editor testing with simulated touch). Phones always show them.</summary>
        public static bool TouchControls
        {
            get => PlayerPrefs.GetInt("plantgame.touch", 0) == 1;
            set { PlayerPrefs.SetInt("plantgame.touch", value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        /// <summary>Touch look speed multiplier (1 = default).</summary>
        public static float TouchLookSensitivity
        {
            get => PlayerPrefs.GetFloat("plantgame.touchlook", 1f);
            set { PlayerPrefs.SetFloat("plantgame.touchlook", Mathf.Clamp(value, 0.3f, 3f)); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        /// <summary>Touch look: dragging up looks down.</summary>
        public static bool TouchInvertY
        {
            get => PlayerPrefs.GetInt("plantgame.touchinvy", 0) == 1;
            set { PlayerPrefs.SetInt("plantgame.touchinvy", value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        /// <summary>Music volume 0..1 (the slider in the menus).</summary>
        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicVolKey, 0.5f);
            set { PlayerPrefs.SetFloat(MusicVolKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        /// <summary>Sound effects + ambience volume 0..1.</summary>
        public static float SoundVolume
        {
            get => PlayerPrefs.GetFloat(SoundVolKey, 0.5f);
            set { PlayerPrefs.SetFloat(SoundVolKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        /// <summary>What the audio systems multiply by: the slider on a squared curve (so the slider has a big audible range) scaled so the 50% default is 20% quieter than before.</summary>
        public static float MusicGain => Mathf.Min(1f, 1.28f * MusicVolume * MusicVolume);
        /// <summary>Sound effects + ambience gain (see MusicGain).</summary>
        public static float SoundGain => Mathf.Min(1f, 1.6f * SoundVolume * SoundVolume);

        /// <summary>Background music. On by default. The audio systems (T17/T18) read this and listen to Changed.</summary>
        public static bool Music
        {
            get => PlayerPrefs.GetInt(MusicKey, 1) == 1;
            set { PlayerPrefs.SetInt(MusicKey, value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        /// <summary>
        /// Post-processing effects (glow, anti-aliasing, distance blur, vignette); the menus label it "Postprocessing". On by default on desktop,
        /// off on phones and tablets. The Atmosphere (T16) reads it. The property keeps its old name (and PlayerPrefs key) so saved choices survive.
        /// </summary>
        public static bool Bloom
        {
            get => PlayerPrefs.GetInt(BloomKey, Application.isMobilePlatform ? 0 : 1) == 1;
            set { PlayerPrefs.SetInt(BloomKey, value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        /// <summary>FPS counter, top right. Off by default.</summary>
        public static bool Fps
        {
            get => PlayerPrefs.GetInt(FpsKey, 0) == 1;
            set { PlayerPrefs.SetInt(FpsKey, value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        /// <summary>Battery saver: caps the frame rate at 30 (QualityTier's first-run preset turns it on for every phone). Applied by MobileLifecycle.</summary>
        public static bool Fps30
        {
            get => PlayerPrefs.GetInt(Fps30Key, 0) == 1;
            set { PlayerPrefs.SetInt(Fps30Key, value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        /// <summary>
        /// Graphics quality: 0 Low, 1 Medium, 2 High. Phones default to Medium (tuned for a 2022 phone at 1080 x 2340), desktop to High.
        /// Applied by QualityTier: the quality level (render scale, HDR, shadows) at once, the world pass (batching, decoration) on the next level.
        /// </summary>
        public static int Quality
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(QualityKey, Application.isMobilePlatform ? 1 : 2), 0, 2);
            set { PlayerPrefs.SetInt(QualityKey, Mathf.Clamp(value, 0, 2)); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        /// <summary>Auto water: the can refills at water within reach and waters thirsty sprouts within reach without F. Off by default.</summary>
        public static bool AutoWater
        {
            get => PlayerPrefs.GetInt(AutoWaterKey, 1) == 1;
            set { PlayerPrefs.SetInt(AutoWaterKey, value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        /// <summary>Sound effects. On by default.</summary>
        public static bool Sound
        {
            get => PlayerPrefs.GetInt(SoundKey, 1) == 1;
            set { PlayerPrefs.SetInt(SoundKey, value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); }
        }
    }
}
