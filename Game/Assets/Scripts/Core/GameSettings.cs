using System;
using UnityEngine;

namespace PlantGame.Core
{
    /// <summary>
    /// Player options persisted in PlayerPrefs (the title Options screen and the pause menu change them). Each value is read from
    /// PlayerPrefs once and then kept here: audio, input and the HUD ask for them every frame.
    /// </summary>
    public static class GameSettings
    {
        const string HintsKey = "plantgame.hints", MusicKey = "plantgame.music", FollowKey = "plantgame.camfollow", SoundKey = "plantgame.sound", MusicVolKey = "plantgame.musicvol", SoundVolKey = "plantgame.soundvol", BloomKey = "plantgame.bloom", FpsKey = "plantgame.fps", AutoWaterKey = "plantgame.autowater", Fps30Key = "plantgame.fps30", QualityKey = "plantgame.quality", DifficultyKey = "plantgame.difficulty", TouchKey = "plantgame.touch", TouchLookKey = "plantgame.touchlook", TouchInvertKey = "plantgame.touchinvy";
        public static event Action Changed;

        static int? _hints, _difficulty, _follow, _touch, _touchInvert, _music, _bloom, _fps, _fps30, _quality, _autoWater, _sound;
        static float? _touchLook, _musicVol, _soundVol;

        /// <summary>Drops the kept values so the next read asks PlayerPrefs again (statics survive entering play mode when domain reload is off).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Forget()
        {
            _hints = _difficulty = _follow = _touch = _touchInvert = _music = _bloom = _fps = _fps30 = _quality = _autoWater = _sound = null;
            _touchLook = _musicVol = _soundVol = null;
        }

        static int Int(ref int? kept, string key, int fallback) => kept ??= PlayerPrefs.GetInt(key, fallback);
        static bool Flag(ref int? kept, string key, bool fallback) => Int(ref kept, key, fallback ? 1 : 0) == 1;
        static float Float(ref float? kept, string key, float fallback) => kept ??= PlayerPrefs.GetFloat(key, fallback);
        static void Set(ref int? kept, string key, int value, bool notify = true) { kept = value; PlayerPrefs.SetInt(key, value); PlayerPrefs.Save(); if (notify) Changed?.Invoke(); }
        static void Set(ref int? kept, string key, bool value) => Set(ref kept, key, value ? 1 : 0);
        static void Set(ref float? kept, string key, float value) { kept = value; PlayerPrefs.SetFloat(key, value); PlayerPrefs.Save(); Changed?.Invoke(); }

        /// <summary>On-screen key hints ("Q pick up / drop, E use, F water"). Off by default.</summary>
        public static bool Hints
        {
            get => Flag(ref _hints, HintsKey, false);
            set => Set(ref _hints, HintsKey, value);
        }

        /// <summary>Difficulty of the last generated level (0 easy, 1 normal, 2 hard); the Generate screen opens on it.</summary>
        public static int Difficulty
        {
            get => Mathf.Clamp(Int(ref _difficulty, DifficultyKey, 1), 0, 2);
            set => Set(ref _difficulty, DifficultyKey, Mathf.Clamp(value, 0, 2), false);
        }

        /// <summary>Third-person camera turns with the player (A / D while moving forward steer it). On by default.</summary>
        public static bool CameraFollow
        {
            get => Application.isMobilePlatform || TouchControls || Flag(ref _follow, FollowKey, true);   // touch: always on
            set => Set(ref _follow, FollowKey, value);
        }

        /// <summary>Show the on-screen touch controls on a non-mobile device (editor testing with simulated touch). Phones always show them.</summary>
        public static bool TouchControls
        {
            get => Flag(ref _touch, TouchKey, false);
            set => Set(ref _touch, TouchKey, value);
        }

        /// <summary>Touch look speed multiplier (1 = default).</summary>
        public static float TouchLookSensitivity
        {
            get => Float(ref _touchLook, TouchLookKey, 1f);
            set => Set(ref _touchLook, TouchLookKey, Mathf.Clamp(value, 0.3f, 3f));
        }

        /// <summary>Touch look: dragging up looks down.</summary>
        public static bool TouchInvertY
        {
            get => Flag(ref _touchInvert, TouchInvertKey, false);
            set => Set(ref _touchInvert, TouchInvertKey, value);
        }

        /// <summary>Music volume 0..1 (the slider in the menus).</summary>
        public static float MusicVolume
        {
            get => Float(ref _musicVol, MusicVolKey, 0.5f);
            set => Set(ref _musicVol, MusicVolKey, Mathf.Clamp01(value));
        }

        /// <summary>Sound effects + ambience volume 0..1.</summary>
        public static float SoundVolume
        {
            get => Float(ref _soundVol, SoundVolKey, 0.5f);
            set => Set(ref _soundVol, SoundVolKey, Mathf.Clamp01(value));
        }

        /// <summary>What the audio systems multiply by: the slider on a squared curve (so the slider has a big audible range), scaled so the 50% default plays at 32% gain.</summary>
        public static float MusicGain => Mathf.Min(1f, 1.28f * MusicVolume * MusicVolume);
        /// <summary>Sound effects + ambience gain (see MusicGain).</summary>
        public static float SoundGain => Mathf.Min(1f, 1.6f * SoundVolume * SoundVolume);

        /// <summary>Background music. On by default. The audio systems read this and listen to Changed.</summary>
        public static bool Music
        {
            get => Flag(ref _music, MusicKey, true);
            set => Set(ref _music, MusicKey, value);
        }

        /// <summary>
        /// Post-processing effects (glow, anti-aliasing, distance blur, vignette); the menus label it "Postprocessing". On by default on desktop,
        /// off on phones and tablets. The Atmosphere reads it. The PlayerPrefs key keeps the old name "plantgame.bloom" so saved choices survive.
        /// </summary>
        public static bool Postprocessing
        {
            get => Flag(ref _bloom, BloomKey, !Application.isMobilePlatform);
            set => Set(ref _bloom, BloomKey, value);
        }

        /// <summary>FPS counter, top right. Off by default.</summary>
        public static bool Fps
        {
            get => Flag(ref _fps, FpsKey, false);
            set => Set(ref _fps, FpsKey, value);
        }

        /// <summary>Battery saver: caps the frame rate at 30 (QualityTier's first-run preset turns it on for every phone). Applied by MobileLifecycle.</summary>
        public static bool Fps30
        {
            get => Flag(ref _fps30, Fps30Key, false);
            set => Set(ref _fps30, Fps30Key, value);
        }

        /// <summary>
        /// Graphics quality: 0 Low, 1 Medium, 2 High. Phones default to Medium (tuned for a 2022 phone at 1080 x 2340), desktop to High.
        /// Applied by QualityTier: the quality level (render scale, HDR, shadows) at once, the world pass (batching, decoration) on the next level.
        /// </summary>
        public static int Quality
        {
            get => Mathf.Clamp(Int(ref _quality, QualityKey, Application.isMobilePlatform ? 1 : 2), 0, 2);
            set => Set(ref _quality, QualityKey, Mathf.Clamp(value, 0, 2));
        }

        /// <summary>Auto water: the can refills at water within reach and waters thirsty sprouts within reach without F. On by default.</summary>
        public static bool AutoWater
        {
            get => Flag(ref _autoWater, AutoWaterKey, true);
            set => Set(ref _autoWater, AutoWaterKey, value);
        }

        /// <summary>Sound effects. On by default.</summary>
        public static bool Sound
        {
            get => Flag(ref _sound, SoundKey, true);
            set => Set(ref _sound, SoundKey, value);
        }
    }
}
