using UnityEngine;
using PlantGame.Core;

namespace PlantGame.Audio
{
    /// <summary>T43: a quiet, distance-limited 3D loop on something that makes that sound (a pond, a waterfall, leafy trees). Silent on the title screen; follows the Sound setting.</summary>
    public class LoopEmitter : MonoBehaviour
    {
        public string clipName; public float volume = 0.4f, minDistance = 2f, maxDistance = 18f;
        AudioSource _src;

        void Start()
        {
            if (AudioManager.OnTitle) { enabled = false; return; }
            var clip = Resources.Load<AudioClip>("Audio/" + clipName);
            if (clip == null) { enabled = false; return; }
            _src = gameObject.AddComponent<AudioSource>();
            _src.clip = clip; _src.loop = true; _src.spatialBlend = 1f; _src.rolloffMode = AudioRolloffMode.Linear;
            _src.minDistance = minDistance; _src.maxDistance = maxDistance; _src.dopplerLevel = 0f;
            var r = new System.Random(unchecked((int)(transform.position.x * 131f + transform.position.z * 977f)));
            _src.pitch = 0.95f + (float)r.NextDouble() * 0.1f; _src.time = (float)r.NextDouble() * clip.length;
            _src.Play();
        }

        void Update() => Apply();
        void OnEnable() => GameSettings.Changed += Apply;   // the pause menu changes the option with the game frozen: apply at once
        void OnDisable() => GameSettings.Changed -= Apply;
        void Apply() { if (_src != null) _src.volume = volume * GameSettings.SoundGain * (GameSettings.Sound ? 1f : 0f); }
    }
}
