using System;
using UnityEngine;

namespace PlantGame.Core
{
    /// <summary>Player options persisted in PlayerPrefs (the pause menu toggles them). Minimal until the T19/T26 options screen.</summary>
    public static class GameSettings
    {
        const string HintsKey = "plantgame.hints", MusicKey = "plantgame.music", FollowKey = "plantgame.camfollow", SoundKey = "plantgame.sound", MusicVolKey = "plantgame.musicvol", SoundVolKey = "plantgame.soundvol", BloomKey = "plantgame.bloom", FpsKey = "plantgame.fps";
        public static event Action Changed;

        /// <summary>On-screen key hints ("Q pick up / drop, E use, F water"). On by default.</summary>
        public static bool Hints
        {
            get => PlayerPrefs.GetInt(HintsKey, 1) == 1;
            set { PlayerPrefs.SetInt(HintsKey, value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        /// <summary>Third-person camera turns with the player (A / D while moving forward steer it). On by default.</summary>
        public static bool CameraFollow
        {
            get => PlayerPrefs.GetInt(FollowKey, 1) == 1;
            set { PlayerPrefs.SetInt(FollowKey, value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        /// <summary>Music volume 0..1 (the slider in the menus).</summary>
        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicVolKey, 0.8f);
            set { PlayerPrefs.SetFloat(MusicVolKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        /// <summary>Sound effects + ambience volume 0..1.</summary>
        public static float SoundVolume
        {
            get => PlayerPrefs.GetFloat(SoundVolKey, 1f);
            set { PlayerPrefs.SetFloat(SoundVolKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        /// <summary>Background music. On by default. The audio systems (T17/T18) read this and listen to Changed.</summary>
        public static bool Music
        {
            get => PlayerPrefs.GetInt(MusicKey, 1) == 1;
            set { PlayerPrefs.SetInt(MusicKey, value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        /// <summary>Glow (bloom) post effect; turn on for a softer look. Off by default. The Atmosphere (T16) reads it.</summary>
        public static bool Bloom
        {
            get => PlayerPrefs.GetInt(BloomKey, 0) == 1;
            set { PlayerPrefs.SetInt(BloomKey, value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        /// <summary>FPS counter, top right. Off by default.</summary>
        public static bool Fps
        {
            get => PlayerPrefs.GetInt(FpsKey, 0) == 1;
            set { PlayerPrefs.SetInt(FpsKey, value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); }
        }

        /// <summary>Sound effects. On by default.</summary>
        public static bool Sound
        {
            get => PlayerPrefs.GetInt(SoundKey, 1) == 1;
            set { PlayerPrefs.SetInt(SoundKey, value ? 1 : 0); PlayerPrefs.Save(); Changed?.Invoke(); }
        }
    }
}
