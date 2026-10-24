using UnityEngine;
using PlantGame.Core;
using PlantGame.Player;
using PlantGame.Audio;

namespace PlantGame.World
{
    /// <summary>
    /// T50: gusts of wind on windy levels. Every 6-13 s a gust ramps up, blows for about 3 s along a slowly turning direction and fades;
    /// it pushes the player (gliding more, standing still little) and is stronger the higher above the start island they are. Not applied
    /// while climbing. A short hint appears at the first gust. Cosmetic sound: the gust raises the wind ambience.
    /// </summary>
    public class WindGusts : MonoBehaviour
    {
        public float maxStandPush = 2.2f, maxAirPush = 4.5f;
        WeatherController _weather;
        PlayerController _pc; float _startY, _next, _t, _len, _dir; bool _hinted; float _strength;

        void Start() { _next = Time.time + Random.Range(4f, 8f); _dir = Random.Range(0f, 360f); }

        void Update()
        {
            if (_pc == null)
            {
                _pc = PlayerController.Instance;
                if (_pc == null) return;
                _startY = _pc.transform.position.y;
            }
            float gust = 0f;
            if (_len > 0f)
            {
                _t += Time.deltaTime;
                float k = _t / _len;
                gust = Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI); gust *= gust;
                if (k >= 1f) { _len = 0f; _next = Time.time + Random.Range(6f, 13f); }
            }
            else if (Time.time >= _next) { _len = Random.Range(2.5f, 4f); _t = 0f; _dir += Random.Range(-70f, 70f); if (!_hinted) { _hinted = true; GameEvents.RaiseHint("A gust of wind! It pushes harder the higher you climb"); } }
            float height = Mathf.Clamp01((_pc.transform.position.y - _startY) / 25f);
            float altitude = Mathf.Lerp(0.35f, 1f, height);
            float push = (_pc.IsAirborne ? maxAirPush : maxStandPush) * altitude * gust;
            _strength = gust;
            var d = Quaternion.Euler(0, _dir, 0) * Vector3.forward;
            if (_weather == null) _weather = GetComponent<WeatherController>();
            var wl = _weather != null ? _weather.WindLeaves : null;
            if (wl != null)   // the blown leaves show where the gust comes from and how hard
            {
                var vel = wl.velocityOverLifetime; float speed = 1.5f + gust * 9f;
                vel.x = d.x * speed; vel.z = d.z * speed; vel.y = -0.9f;
                var em = wl.emission; em.rateOverTime = 14f + gust * 110f;
            }
            _pc.WindVelocity = _pc.IsClimbing ? Vector3.zero : d * push;
            var am = AudioManager.Instance;   // T46: the wind roars louder, higher and with each gust the higher you are
            if (am != null) { am.AmbienceBoost = Mathf.Lerp(0.55f, 1.7f, height) * (1f + 0.5f * gust); am.AmbiencePitch = Mathf.Lerp(0.88f, 1.18f, height) + 0.06f * gust; }
        }

        void OnDestroy() { if (_pc != null) _pc.WindVelocity = Vector3.zero; if (AudioManager.Instance != null) { AudioManager.Instance.AmbienceBoost = 1f; AudioManager.Instance.AmbiencePitch = 1f; } }

        /// <summary>0..1: how hard the current gust blows (for effects and sound).</summary>
        public float Strength => _strength;
    }
}
