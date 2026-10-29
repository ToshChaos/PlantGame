using UnityEngine;
using PlantGame.Core;

namespace PlantGame.Audio
{
    /// <summary>
    /// T43: faint, mysterious sounds near ruins: a low drone, a whisper, stone resonating, distant chimes. One at a time, on a slow random
    /// schedule, and only while the player is within `radius` of the ruin.
    /// </summary>
    public class RuinSounds : MonoBehaviour
    {
        public float radius = 22f, volume = 0.45f;
        static readonly string[] Names = { "ruin_drone_1", "ruin_drone_2", "ruin_whisper_1", "ruin_whisper_2", "ruin_stone_1", "ruin_stone_2", "ruin_chime_1", "ruin_chime_2" };
        AudioSource _src; AudioClip[] _clips; float _next; Transform _player;

        void Start()
        {
            if (AudioManager.OnTitle) { enabled = false; return; }
            _clips = new AudioClip[Names.Length];
            for (int i = 0; i < Names.Length; i++) _clips[i] = Resources.Load<AudioClip>("Audio/" + Names[i]);
            _src = gameObject.AddComponent<AudioSource>();
            _src.spatialBlend = 1f; _src.rolloffMode = AudioRolloffMode.Linear; _src.minDistance = 3f; _src.maxDistance = radius; _src.dopplerLevel = 0f;
            _next = Time.time + Random.Range(3f, 9f);
        }

        void OnEnable() => GameSettings.Changed += Apply;   // applied at once, also while the game is paused
        void OnDisable() => GameSettings.Changed -= Apply;
        void Apply() { if (_src != null) _src.volume = volume * GameSettings.SoundGain * (GameSettings.Sound ? 1f : 0f); }

        void Update()
        {
            if (_src == null) return;
            Apply();
            if (Time.time < _next || _src.isPlaying) return;
            if (_player == null) { var pc = Player.PlayerController.Instance; if (pc != null) _player = pc.transform; else return; }
            if ((_player.position - transform.position).sqrMagnitude > radius * radius) { _next = Time.time + 2f; return; }
            _next = Time.time + Random.Range(7f, 18f);
            var c = _clips[Random.Range(0, _clips.Length)]; if (c == null) return;
            _src.pitch = Random.Range(0.92f, 1.08f); _src.PlayOneShot(c);
        }
    }
}
