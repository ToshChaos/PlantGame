using UnityEngine;
using PlantGame.Core;
using PlantGame.Inventory;

namespace PlantGame.Audio
{
    /// <summary>A quiet 3D loop (rustling leaves, a few chimes) on trees and patches that hand out seeds, so you can hear where they are. Follows the Sound setting.</summary>
    [RequireComponent(typeof(SeedTree))]
    public class SeedAmbience : MonoBehaviour
    {
        const float Loudness = 0.5f;
        AudioSource _src;

        void Awake()
        {
            var clip = Resources.Load<AudioClip>("Audio/seed_tree_ambience");
            if (clip == null) { enabled = false; return; }
            _src = gameObject.AddComponent<AudioSource>();
            _src.clip = clip; _src.loop = true; _src.spatialBlend = 1f; _src.rolloffMode = AudioRolloffMode.Linear;
            _src.minDistance = 2f; _src.maxDistance = 16f; _src.dopplerLevel = 0f;
            var r = new System.Random(unchecked((int)(transform.position.x * 131f + transform.position.z * 977f)));
            _src.pitch = 0.9f + (float)r.NextDouble() * 0.2f;     // every tree sounds a little different
            _src.time = (float)r.NextDouble() * clip.length;
            _src.Play();
        }

        void Update() => Apply();
        void OnEnable() => GameSettings.Changed += Apply;   // applied at once, also while the game is paused
        void OnDisable() => GameSettings.Changed -= Apply;
        void Apply() { if (_src != null) _src.volume = Loudness * GameSettings.SoundGain * (GameSettings.Sound ? 1f : 0f); }
    }
}
