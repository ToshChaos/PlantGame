using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlantGame.Core;

namespace PlantGame.Audio
{
    /// <summary>
    /// T17: music crossfade (menu / gameplay), looping ambience and pooled one-shot SFX. Clips are looked up by name
    /// (`footstep_grass` picks randomly among `footstep_grass_1..n`). Music and Sound follow GameSettings.
    /// Volumes are plain per-bus floats here (no AudioMixer asset: the editor API to author one is internal).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Serializable] public class Entry { public string name; public AudioClip[] clips; }

        public static AudioManager Instance { get; private set; }

        public AudioClip menuMusic, gameplayMusic, windAmbience;
        [Tooltip("Per-theme tracks (LevelTheme.Rain / Autumn); Sunny uses gameplayMusic + windAmbience.")]
        public AudioClip rainMusic, autumnMusic, rainAmbience;
        [Tooltip("T37: calm one-shot piece played (not looped) when the goal is reached.")]
        public AudioClip winMusic;
        [Tooltip("T53: one tune per curated level (Entry.name = levelId, first clip). Level 1, random and seed-code levels use gameplayMusic.")]
        public List<Entry> levelMusic = new List<Entry>();
        public List<Entry> sfx = new List<Entry>();
        [Range(0, 1)] public float masterVolume = 1f, musicVolume = 0.55f, sfxVolume = 0.8f, ambienceVolume = 0.175f;
        public float crossfadeSeconds = 2.5f;
        public int voices = 12;

        readonly Dictionary<string, AudioClip[]> _lib = new Dictionary<string, AudioClip[]>();
        readonly Dictionary<string, float> _last = new Dictionary<string, float>();
        AudioSource[] _music = new AudioSource[2];
        AudioSource _amb;
        /// <summary>T46: windy levels scale the ambience loop with altitude and gusts (1 = normal).</summary>
        public float AmbienceBoost = 1f, AmbiencePitch = 1f;
        AudioSource[] _pool;
        int _poolNext, _active;
        AudioClip _target;
        float _fade = 1f;

        void Awake()
        {
            Instance = this;
            foreach (var e in sfx) if (!string.IsNullOrEmpty(e.name) && e.clips != null && e.clips.Length > 0) _lib[e.name] = e.clips;
            for (int i = 0; i < 2; i++) _music[i] = NewSource("Music" + i, true);
            _amb = NewSource("Ambience", true);
            _pool = new AudioSource[voices];
            for (int i = 0; i < voices; i++) _pool[i] = NewSource("Sfx" + i, false);
            ApplyThemeAmbience();
        }

        public static bool OnTitle => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == UI.PlayerHud.TitleScene;

        LevelTheme Theme => WorldTheme.Resolve(GameBootstrap.Current);

        void ApplyThemeAmbience()
        {
            if (OnTitle) { _amb.Stop(); _amb.clip = null; return; }   // the title plays the music only
            var clip = Theme == LevelTheme.Rain && rainAmbience != null ? rainAmbience : windAmbience;
            if (clip == null || _amb.clip == clip) return;
            _amb.clip = clip; _amb.Play();
        }

        AudioClip LevelTune(string id)
        {
            foreach (var e in levelMusic) if (e.name == id && e.clips != null && e.clips.Length > 0) return e.clips[0];
            return null;
        }

        AudioClip GameplayTrack()
        {
            var t = Theme;
            string id = GameBootstrap.Current.levelId;
            if (!string.IsNullOrEmpty(id)) foreach (var e in levelMusic) if (e.name == id && e.clips != null && e.clips.Length > 0 && e.clips[0] != null) return e.clips[0];   // T53
            // random / seed-code levels: match the tune to the theme (reusing the curated level tunes)
            switch (t)
            {
                case LevelTheme.Rain: if (rainMusic != null) return rainMusic; break;
                case LevelTheme.Autumn: if (autumnMusic != null) return autumnMusic; break;
                case LevelTheme.Snow: return LevelTune("level_7") ?? menuMusic ?? gameplayMusic;
                case LevelTheme.Fog: return LevelTune("level_9") ?? rainMusic ?? gameplayMusic;
                case LevelTheme.Sunrise: return LevelTune("level_6") ?? gameplayMusic;
                case LevelTheme.Sunset: return LevelTune("level_5") ?? gameplayMusic;
            }
            return gameplayMusic;
        }

        void OnEnable()
        {
            GameSettings.Changed += ApplyVolumes;
            GameEvents.RunStarted += OnRunStarted;
            GameEvents.GameWon += OnWon;
        }

        void OnDisable()
        {
            GameSettings.Changed -= ApplyVolumes;
            GameEvents.RunStarted -= OnRunStarted;
            GameEvents.GameWon -= OnWon;
        }

        // RunSession.Start may already have raised RunStarted (and picked the level tune) before this Start runs: never overwrite it with the menu tune
        void Start() { if (_target == null) PlayMusic(OnTitle ? menuMusic : GameplayTrack()); ApplyVolumes(); }
        void OnDestroy() { if (Instance == this) Instance = null; }

        float _holdUntil; bool _holding;
        void OnRunStarted()
        {
            ApplyThemeAmbience(); PlayMusic(GameplayTrack());
            if (OnTitle || !GameSettings.Sound || !_lib.TryGetValue("level_start", out var jingle)) return;
            Play("level_start", 0.9f, 0f, 0f);   // the level-start jingle: the music pauses until it has played
            _holdUntil = Time.unscaledTime + jingle[0].length; _holding = true;
        }
        void OnWon() { if (winMusic != null) PlayMusic(winMusic, false); else _musicDuck = 0.45f; }
        float _musicDuck = 1f;

        AudioSource NewSource(string n, bool loop)
        {
            var go = new GameObject(n); go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false; s.loop = loop; s.spatialBlend = 0f;
            return s;
        }

        // ---------------------------------------------------------------- music
        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (clip == null || clip == _target) return;
            _target = clip;
            var next = _music[1 - _active];
            next.clip = clip; next.loop = loop; next.volume = 0f; next.Play();
            _active = 1 - _active;
            _fade = 0f;
            _musicDuck = 1f;
        }

        void Update()
        {
            if (_holding)
            {
                if (Time.unscaledTime < _holdUntil) { _music[0].Pause(); _music[1].Pause(); }
                else { _holding = false; _music[0].UnPause(); _music[1].UnPause(); }
            }
            if (_fade < 1f && !(_holding && Time.unscaledTime < _holdUntil)) _fade = Mathf.Min(1f, _fade + Time.unscaledDeltaTime / Mathf.Max(0.1f, crossfadeSeconds));
            if (OnTitle) { if (_amb.isPlaying) _amb.Stop(); _amb.clip = null; }
            else if (_amb.clip == null) ApplyThemeAmbience();
            ApplyVolumes();
            _amb.pitch = AmbiencePitch;
        }
        float _duckSmooth = 1f;

        /// <summary>Music / ambience volumes from the settings; also runs the moment an option changes (the pause menu is open with the game frozen).</summary>
        void ApplyVolumes()
        {
            float level = (GameSettings.Music ? 1f : 0f) * GameSettings.MusicGain * musicVolume * masterVolume;
            _duckSmooth = Mathf.MoveTowards(_duckSmooth, _musicDuck, Time.unscaledDeltaTime * 0.5f);
            var cur = _music[_active]; var old = _music[1 - _active];
            cur.volume = Mathf.Sqrt(_fade) * level * _duckSmooth;
            old.volume = Mathf.Sqrt(1f - _fade) * level;
            if (_fade >= 1f && old.isPlaying) old.Stop();
            float amb = Mathf.Clamp01(ambienceVolume * AmbienceBoost) * (Theme == LevelTheme.Rain ? Mathf.Lerp(0.25f, 1f, WeatherState.Intensity) : 1f) * masterVolume * GameSettings.SoundGain;
            _amb.volume = GameSettings.Sound && !float.IsNaN(amb) ? amb : 0f;   // a NaN would be ignored by the source and leave the old volume playing
            _amb.mute = !GameSettings.Sound;
            _music[0].mute = _music[1].mute = !GameSettings.Music;
            if (!GameSettings.Sound) foreach (var v in _pool) if (v.isPlaying) v.Stop();   // switching sound off cuts what is playing
        }

        // ---------------------------------------------------------------- sfx
        /// <summary>Plays a named one-shot (random variant, pitch +/-5%). minInterval rate-limits repeats of the same name.</summary>
        public static void PlaySfx(string name, float volume = 1f, float pitchJitter = 0.05f, float minInterval = 0.05f, Vector3? at = null)
        {
            if (Instance != null) Instance.Play(name, volume, pitchJitter, minInterval, at);
        }

        public void Play(string name, float volume = 1f, float pitchJitter = 0.05f, float minInterval = 0.05f, Vector3? at = null)
        {
            if (!GameSettings.Sound || !_lib.TryGetValue(name, out var clips)) return;
            float now = Time.unscaledTime;
            if (_last.TryGetValue(name, out var t) && now - t < minInterval) return;
            _last[name] = now;
            var s = _pool[_poolNext]; _poolNext = (_poolNext + 1) % _pool.Length;
            s.clip = clips[UnityEngine.Random.Range(0, clips.Length)];   // audio variation is cosmetic, not world generation
            s.pitch = 1f + UnityEngine.Random.Range(-pitchJitter, pitchJitter);
            s.volume = volume * sfxVolume * masterVolume * GameSettings.SoundGain;
            if (at.HasValue) { s.transform.position = at.Value; s.spatialBlend = 0.6f; } else s.spatialBlend = 0f;
            s.Play();
        }
    }
}
